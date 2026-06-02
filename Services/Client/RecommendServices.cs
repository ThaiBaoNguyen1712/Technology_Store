using System.Net.Http.Json;
using Tech_Store.Models.DTO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tech_Store.Services.Admin.ProductServices;
using Tech_Store.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Tech_Store.Services.Client.RecommendServices
{
    public class RecommendServices
    {
        private readonly HttpClient _http;
        private readonly ProductServices _productServices;
        private readonly ApplicationDbContext _context;
        private readonly bool _recommendApiEnabled;

        public RecommendServices(HttpClient http, ProductServices productServices, ApplicationDbContext context, IConfiguration configuration)
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
            if (_recommendApiEnabled)
            {
                try
                {
                    var url = BuildRecommendationUrl("homepage", userId, null, topN);
                    // Gọi AI lấy ID
                    var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                    if (aiResult?.Recommendations != null) ids = aiResult.Recommendations;
                }
                catch { /* Log error if needed */ }
            }

            if (!ids.Any())
            {
                // Nếu AI lỗi, lấy Hot Sale từ DB làm Backup
                return new RecommendResponse
                {
                    Scene = "homepage",
                    Products = await _productServices.GetHotSaleProducts(topN)
                };
            }

            return new RecommendResponse
            {
                Scene = "homepage",
                Products = await GetFullProductsFromIds(ids)
            };
        }

        // ===============================
        // 2️⃣ Scene-based (Detail, Cart, Wishlist)
        // ===============================
        public async Task<RecommendResponse> GetSceneRecommend(int userId, string scene, string? productSysId = null, int topN = 15)
        {
            List<string> ids = new();
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
                            Products = await BuildBackupProducts(scene, productSysId, topN)
                        };
                    }

                    var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                    if (aiResult?.Recommendations != null)
                    {
                        ids = aiResult.Recommendations;
                    }
                }
                catch { }
            }

            if (ids.Any())
            {
                return new RecommendResponse
                {
                    Scene = scene,
                    Products = await GetFullProductsFromIds(ids)
                };
            }

            // Nếu AI không có kết quả, chạy logic dự phòng
            return new RecommendResponse
            {
                Scene = scene,
                Products = await BuildBackupProducts(scene, productSysId, topN)
            };
        }

        // ===============================
        // HÀM PHỤ TRỢ: Lấy Product từ ID
        // ===============================
        private async Task<List<Product>> GetFullProductsFromIds(List<string> ids)
        {
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

        private static string? BuildRecommendationUrl(string scene, int userId, string? productSysId, int limit)
        {
            return scene switch
            {
                "detail" when !string.IsNullOrWhiteSpace(productSysId)
                    => $"api/v1/recommendations/users/{userId}/detail/{productSysId}?limit={limit}",
                "cart"
                    => $"api/v1/recommendations/users/{userId}/cart?limit={limit}",
                "wishlist"
                    => $"api/v1/recommendations/users/{userId}/wishlist?limit={limit}",
                "homepage"
                    => $"api/v1/recommendations/users/{userId}/homepage?limit={limit}",
                _ => null
            };
        }
    }
}
