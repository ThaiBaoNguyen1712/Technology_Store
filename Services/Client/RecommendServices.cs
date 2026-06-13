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
                _http.Timeout = TimeSpan.FromSeconds(10);
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
                        var fallback = await BuildBackupProducts("homepage", userId, null, topN);
                        return new RecommendResponse
                        {
                            Scene = "homepage",
                            Products = fallback.Products,
                            DataSource = "fallback",
                            MlAttempted = false,
                            MlSucceeded = false,
                            FallbackUsed = true,
                            FallbackSource = fallback.Source,
                            FallbackReason = userId > 0 ? "unsupported_scene" : "anonymous_user_not_supported",
                            UserId = userId
                        };
                    }

                    mlRequestUrl = url;
                    mlAttempted = true;
                    var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                    ids = ExtractProductSysIds(aiResult);
                    _logger.LogInformation("Recommendation API success. Scene={Scene}, UserId={UserId}, ResultCount={ResultCount}, Url={Url}",
                        "homepage", userId, ids.Count, url);
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
                var fallback = await BuildBackupProducts("homepage", userId, null, topN);
                return new RecommendResponse
                {
                    Scene = "homepage",
                    Products = fallback.Products,
                    DataSource = "fallback",
                    MlAttempted = mlAttempted,
                    MlSucceeded = false,
                    FallbackUsed = true,
                    FallbackSource = fallback.Source,
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
                MlSucceeded = true,
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
                        var fallback = await BuildBackupProducts(scene, userId, productSysId, topN);
                        return new RecommendResponse
                        {
                            Scene = scene,
                            Products = fallback.Products,
                            DataSource = "fallback",
                            MlAttempted = false,
                            MlSucceeded = false,
                            FallbackUsed = true,
                            FallbackSource = fallback.Source,
                            FallbackReason = ResolveUnsupportedReason(scene, userId, productSysId),
                            UserId = userId,
                            ProductSysId = productSysId
                        };
                    }

                    mlRequestUrl = url;
                    mlAttempted = true;
                    var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                    ids = ExtractProductSysIds(aiResult);
                    _logger.LogInformation("Recommendation API success. Scene={Scene}, UserId={UserId}, ProductSysId={ProductSysId}, ResultCount={ResultCount}, Url={Url}",
                        scene, userId, productSysId, ids.Count, url);
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
                    MlSucceeded = true,
                    FallbackUsed = false,
                    MlRequestUrl = mlRequestUrl,
                    UserId = userId,
                    ProductSysId = productSysId,
                    MlResultCount = ids.Count
                };
            }

            // Nếu AI không có kết quả, chạy logic dự phòng
            _logger.LogInformation("Recommendation API returned no items for scene {Scene}, user {UserId}, productSysId {ProductSysId}. Using default query fallback.", scene, userId, productSysId);
            var sceneFallback = await BuildBackupProducts(scene, userId, productSysId, topN);
            return new RecommendResponse
            {
                Scene = scene,
                Products = sceneFallback.Products,
                DataSource = "fallback",
                MlAttempted = mlAttempted,
                MlSucceeded = false,
                FallbackUsed = true,
                FallbackSource = sceneFallback.Source,
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

        private async Task<FallbackRecommendResult> BuildBackupProducts(string scene, int userId, string? productSysId, int topN)
        {
            if (userId > 0)
            {
                var personalizedProducts = await GetInteractionBasedProductsForUserAsync(userId, scene, productSysId, topN);
                if (personalizedProducts.Count > 0)
                {
                    return new FallbackRecommendResult(personalizedProducts, "user_behavior");
                }
            }

            if (scene == "detail" && !string.IsNullOrEmpty(productSysId))
            {
                var interactionProducts = await GetInteractionBasedProductsAsync(topN, productSysId, sameCategoryOnly: true);
                if (interactionProducts.Count > 0)
                {
                    return new FallbackRecommendResult(interactionProducts, "interaction_same_category");
                }

                var relatedProducts = await _productServices.GetRelatedProductsAsync(productSysId, topN);
                if (relatedProducts.Count > 0)
                {
                    return new FallbackRecommendResult(relatedProducts, "related_products");
                }
            }

            var globalInteractionProducts = await GetInteractionBasedProductsAsync(topN, productSysId);
            if (globalInteractionProducts.Count > 0)
            {
                return new FallbackRecommendResult(globalInteractionProducts, "global_interaction");
            }

            return new FallbackRecommendResult(await _productServices.GetHotSaleProducts(topN), "hot_sale");
        }

        private async Task<List<Product>> GetInteractionBasedProductsForUserAsync(int userId, string scene, string? productSysId, int topN)
        {
            var preferredCategoryIds = await _context.UserProductEvents
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Join(
                    _context.Products.AsNoTracking(),
                    ev => ev.ProductId,
                    product => product.ProductId,
                    (ev, product) => new
                    {
                        product.CategoryId,
                        ev.InteractionScore
                    })
                .Where(x => x.CategoryId.HasValue)
                .GroupBy(x => x.CategoryId!.Value)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    Score = g.Sum(x => x.InteractionScore)
                })
                .OrderByDescending(x => x.Score)
                .Take(3)
                .Select(x => x.CategoryId)
                .ToListAsync();

            var interactedProductIds = await _context.UserProductEvents
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => x.ProductId)
                .Distinct()
                .ToListAsync();

            var currentProductId = await ResolveProductIdAsync(productSysId);
            var excludedProductIds = interactedProductIds;
            if (currentProductId.HasValue)
            {
                excludedProductIds.Add(currentProductId.Value);
            }

            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Where(IsAvailableProductPredicate());

            if (preferredCategoryIds.Count > 0)
            {
                query = query.Where(x => x.CategoryId.HasValue && preferredCategoryIds.Contains(x.CategoryId.Value));
            }

            if (excludedProductIds.Count > 0)
            {
                query = query.Where(x => !excludedProductIds.Contains(x.ProductId));
            }

            var products = await query
                .GroupJoin(
                    _context.UserProductEvents.AsNoTracking(),
                    product => product.ProductId,
                    behavior => behavior.ProductId,
                    (product, behaviors) => new
                    {
                        Product = product,
                        InteractionScore = behaviors.Sum(x => (double?)x.InteractionScore) ?? 0d,
                        LastInteractedAt = behaviors.Max(x => (DateTime?)x.LastInteractedAt)
                    })
                .OrderByDescending(x => x.InteractionScore)
                .ThenByDescending(x => x.LastInteractedAt)
                .ThenByDescending(x => x.Product.CreatedAt)
                .Take(topN)
                .Select(x => x.Product)
                .ToListAsync();

            if (products.Count > 0)
            {
                return products;
            }

            return await GetInteractionBasedProductsAsync(topN, productSysId);
        }

        private async Task<List<Product>> GetInteractionBasedProductsAsync(int topN, string? productSysId, bool sameCategoryOnly = false)
        {
            var currentProduct = await ResolveProductAsync(productSysId);
            var currentProductId = currentProduct?.ProductId;
            var currentCategoryId = currentProduct?.CategoryId;

            IQueryable<Product> query = _context.Products
                .AsNoTracking()
                .Where(IsAvailableProductPredicate());

            if (currentProductId.HasValue)
            {
                query = query.Where(x => x.ProductId != currentProductId.Value);
            }

            if (sameCategoryOnly && currentCategoryId.HasValue)
            {
                query = query.Where(x => x.CategoryId == currentCategoryId.Value);
            }

            return await query
                .GroupJoin(
                    _context.UserProductEvents.AsNoTracking(),
                    product => product.ProductId,
                    behavior => behavior.ProductId,
                    (product, behaviors) => new
                    {
                        Product = product,
                        InteractionScore = behaviors.Sum(x => (double?)x.InteractionScore) ?? 0d,
                        LastInteractedAt = behaviors.Max(x => (DateTime?)x.LastInteractedAt)
                    })
                .Where(x => x.InteractionScore > 0)
                .OrderByDescending(x => x.InteractionScore)
                .ThenByDescending(x => x.LastInteractedAt)
                .ThenByDescending(x => x.Product.CreatedAt)
                .Take(topN)
                .Select(x => x.Product)
                .ToListAsync();
        }

        private async Task<Product?> ResolveProductAsync(string? productSysId)
        {
            if (string.IsNullOrWhiteSpace(productSysId))
            {
                return null;
            }

            return await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductSysId == productSysId);
        }

        private async Task<int?> ResolveProductIdAsync(string? productSysId)
        {
            if (string.IsNullOrWhiteSpace(productSysId))
            {
                return null;
            }

            return await _context.Products
                .AsNoTracking()
                .Where(x => x.ProductSysId == productSysId)
                .Select(x => (int?)x.ProductId)
                .FirstOrDefaultAsync();
        }

        private static System.Linq.Expressions.Expression<Func<Product, bool>> IsAvailableProductPredicate()
        {
            return x => x.Visible == true && x.Stock >= 1 && x.Status != "outstock";
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

        private sealed record FallbackRecommendResult(List<Product> Products, string Source);
    }
}
