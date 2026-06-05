using System.Net.Http.Json;
using Tech_Store.Models.DTO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tech_Store.Services.Admin.ProductServices;
using Tech_Store.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Tech_Store.Services.Client.RecommendServices
{
    public class RecommendServices
    {
        private readonly HttpClient _http;
        private readonly ProductServices _productServices;
        private readonly ApplicationDbContext _context;
        private readonly bool _recommendApiEnabled;
        private readonly ILogger<RecommendServices> _logger;

        public RecommendServices(HttpClient http, ProductServices productServices, ApplicationDbContext context, IConfiguration configuration, ILogger<RecommendServices> logger)
        {
            _http = http;
            var recommendApiBaseUrl = configuration["RecommendApi:BaseUrl"];
            _recommendApiEnabled = !string.IsNullOrWhiteSpace(recommendApiBaseUrl);
            if (_recommendApiEnabled)
            {
                _http.BaseAddress = new Uri(recommendApiBaseUrl!);
                _http.Timeout = TimeSpan.FromSeconds(5);
            }
            _productServices = productServices;
            _context = context;
            _logger = logger;
        }

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        // ===============================
        // 1️⃣ Homepage
        // ===============================
        public async Task<RecommendResponse> GetHomepageRecommend(int userId, int topN = 15)
        {
            List<string> ids = new();
            string? mlRequestUrl = null;
            string? mlError = null;
            var mlAttempted = false;
            if (_recommendApiEnabled)
            {
                try
                {
                    var url = BuildRecommendationUrl("homepage", userId, null, topN);
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        return new RecommendResponse
                        {
                            Scene = "homepage",
                            Products = await _productServices.GetHotSaleProducts(topN),
                            DataSource = "fallback",
                            MlAttempted = false,
                            FallbackUsed = true,
                            FallbackReason = userId > 0 ? "unsupported_scene" : "anonymous_user_not_supported",
                            UserId = userId
                        };
                    }

                    mlRequestUrl = url;
                    mlAttempted = true;
                    var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                    ids = ExtractProductSysIds(aiResult);
                }
                catch (Exception ex)
                {
                    mlError = ex.Message;
                    _logger.LogWarning(ex, "Recommendation API call failed for scene {Scene} and user {UserId}. Falling back to default query.", "homepage", userId);
                }
            }

            if (!ids.Any())
            {
                _logger.LogInformation("Recommendation API returned no items for scene {Scene} and user {UserId}. Using default query fallback.", "homepage", userId);
                // Nếu AI lỗi, lấy Hot Sale từ DB làm Backup
                return new RecommendResponse
                {
                    Scene = "homepage",
                    Products = await _productServices.GetHotSaleProducts(topN),
                    DataSource = "fallback",
                    MlAttempted = mlAttempted,
                    FallbackUsed = true,
                    MlRequestUrl = mlRequestUrl,
                    MlError = mlError,
                    FallbackReason = mlAttempted ? "ml_empty_or_failed" : "recommendation_api_disabled",
                    UserId = userId,
                    MlResultCount = 0
                };
            }

            return new RecommendResponse
            {
                Scene = "homepage",
                Products = await GetFullProductsFromIds(ids),
                DataSource = "ml",
                MlAttempted = mlAttempted,
                FallbackUsed = false,
                MlRequestUrl = mlRequestUrl,
                UserId = userId,
                MlResultCount = ids.Count
            };
        }

        // ===============================
        // 2️⃣ Scene-based (Detail, Cart, Wishlist)
        // ===============================
        public async Task<RecommendResponse> GetSceneRecommend(int userId, string scene, string? productSysId = null, int topN = 15)
        {
            List<string> ids = new();
            string? mlRequestUrl = null;
            string? mlError = null;
            var mlAttempted = false;
            if (_recommendApiEnabled)
            {
                try
                {
                    var url = BuildRecommendationUrl(scene, userId, productSysId, topN);

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        return new RecommendResponse
                        {
                            Scene = scene,
                            Products = await BuildBackupProducts(scene, productSysId, topN),
                            DataSource = "fallback",
                            MlAttempted = false,
                            FallbackUsed = true,
                            FallbackReason = ResolveUnsupportedReason(scene, userId, productSysId),
                            UserId = userId,
                            ProductSysId = productSysId
                        };
                    }

                    mlRequestUrl = url;
                    mlAttempted = true;
                    var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                    ids = ExtractProductSysIds(aiResult);
                }
                catch (Exception ex)
                {
                    mlError = ex.Message;
                    _logger.LogWarning(ex, "Recommendation API call failed for scene {Scene}, user {UserId}, productSysId {ProductSysId}. Falling back to default query.", scene, userId, productSysId);
                }
            }

            if (ids.Any())
            {
                return new RecommendResponse
                {
                    Scene = scene,
                    Products = await GetFullProductsFromIds(ids),
                    DataSource = "ml",
                    MlAttempted = mlAttempted,
                    FallbackUsed = false,
                    MlRequestUrl = mlRequestUrl,
                    UserId = userId,
                    ProductSysId = productSysId,
                    MlResultCount = ids.Count
                };
            }

            // Nếu AI không có kết quả, chạy logic dự phòng
            _logger.LogInformation("Recommendation API returned no items for scene {Scene}, user {UserId}, productSysId {ProductSysId}. Using default query fallback.", scene, userId, productSysId);
            return new RecommendResponse
            {
                Scene = scene,
                Products = await BuildBackupProducts(scene, productSysId, topN),
                DataSource = "fallback",
                MlAttempted = mlAttempted,
                FallbackUsed = true,
                MlRequestUrl = mlRequestUrl,
                MlError = mlError,
                FallbackReason = mlAttempted ? "ml_empty_or_failed" : "recommendation_api_disabled",
                UserId = userId,
                ProductSysId = productSysId,
                MlResultCount = 0
            };
        }

        // ===============================
        // HÀM PHỤ TRỢ: Lấy Product từ ID
        // ===============================
        private async Task<List<Product>> GetFullProductsFromIds(List<string> ids)
        {
            ids = ids
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Lấy toàn bộ sản phẩm có trong danh sách ID từ Database
            var products = await _context.Products
                .Where(p => ids.Contains(p.ProductSysId))
                .ToListAsync();

            // Sắp xếp lại danh sách Product ĐÚNG THỨ TỰ mà AI đã trả về
            return ids.Select(id => products.FirstOrDefault(p => p.ProductSysId == id))
                      .Where(p => p != null)
                      .ToList()!;
        }

        private async Task<List<Product>> BuildBackupProducts(string scene, string? productSysId, int topN)
        {
            if (scene == "detail" && !string.IsNullOrEmpty(productSysId))
            {
                return await _productServices.GetRelatedProductsAsync(productSysId, topN);
            }
            return await _productServices.GetHotSaleProducts(topN);
        }

        private static List<string> ExtractProductSysIds(RecommendRawResponse? response)
        {
            if (response?.RecommendationItems?.Count > 0)
            {
                return response.RecommendationItems
                    .Select(x => x.ProductSysId)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            return response?.Recommendations?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
                ?? new List<string>();
        }

        private static string? BuildRecommendationUrl(string scene, int userId, string? productSysId, int limit)
        {
            return scene switch
            {
                "detail" when !string.IsNullOrWhiteSpace(productSysId) && userId <= 0
                    => $"api/v1/recommendations/similar/{productSysId}?limit={limit}",
                "detail" when !string.IsNullOrWhiteSpace(productSysId)
                    => $"api/v1/recommendations/users/{userId}/detail/{productSysId}?limit={limit}",
                "cart" when userId <= 0
                    => null,
                "cart"
                    => $"api/v1/recommendations/users/{userId}/cart?limit={limit}",
                "wishlist" when userId <= 0
                    => null,
                "wishlist"
                    => $"api/v1/recommendations/users/{userId}/wishlist?limit={limit}",
                "homepage" when userId <= 0
                    => null,
                "homepage"
                    => $"api/v1/recommendations/users/{userId}/homepage?limit={limit}",
                _ => null
            };
        }

        private static string ResolveUnsupportedReason(string scene, int userId, string? productSysId)
        {
            if (scene == "detail" && string.IsNullOrWhiteSpace(productSysId))
            {
                return "missing_product_sys_id";
            }

            if ((scene == "homepage" || scene == "cart" || scene == "wishlist") && userId <= 0)
            {
                return "anonymous_user_not_supported";
            }

            return "unsupported_scene";
        }
    }
}
