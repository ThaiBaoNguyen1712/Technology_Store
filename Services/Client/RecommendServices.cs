using System.Net.Http.Json;
using Tech_Store.Models.DTO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Tech_Store.Services.Admin.ProductServices;
using Tech_Store.Models;
using Microsoft.EntityFrameworkCore;

namespace Tech_Store.Services.Client.RecommendServices
{
    public class RecommendServices
    {
        private readonly HttpClient _http;
        private readonly ProductServices _productServices;
        private readonly ApplicationDbContext _context;

        public RecommendServices(HttpClient http, ProductServices productServices, ApplicationDbContext context)
        {
            _http = http;
        //http://ml-api:8000/
             //http://127.0.0.1:8000/
             //_http.BaseAddress = new Uri("");
            _http.BaseAddress = new Uri("http://127.0.0.1:8000/"); 
            _http.Timeout = TimeSpan.FromSeconds(5);
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
        public async Task<RecommendResponse> GetHomepageRecommend(int userId, int topN = 50)
        {
            List<string> ids = new();
            try
            {
                var url = $"api/v1/recommendation/homepage/{userId}?top_n={topN}";
                // Gọi AI lấy ID
                var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                if (aiResult?.Recommendations != null) ids = aiResult.Recommendations;
            }
            catch { /* Log error if needed */ }

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
        public async Task<RecommendResponse> GetSceneRecommend(int userId, string scene, string? productSysId = null, int topN = 10)
        {
            List<string> ids = new();
            try
            {
                string url = scene == "detail"
                    ? $"api/v1/recommendation/{userId}/{scene}/{productSysId}?top_n={topN}"
                    : $"api/v1/recommendation/{userId}/{scene}/_?top_n={topN}";

                var aiResult = await _http.GetFromJsonAsync<RecommendRawResponse>(url, _jsonOptions);
                if (aiResult?.Recommendations != null) ids = aiResult.Recommendations;
            }
            catch { }

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
    }
}