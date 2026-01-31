using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Services.Admin.ProductServices
{
    public class ProductServices
    {
        private readonly ApplicationDbContext _context;
        public ProductServices(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<List<Product>> GetAllProductsAsync()
        {
            return await _context.Products.ToListAsync();
        }

        public async Task<List<Product>> GetHotSaleProducts(int topN = 10)
        {
            return await _context.Products
                .Where(x =>
                    x.Visible == true &&
                    x.Stock >= 1 &&
                    x.Status != "outstock"
                )
                .OrderByDescending(x => x.OrderItems.Count)
                .Take(topN)
                .ToListAsync();
        }

        //Lấy sản phảm liên quan trong cùng category
        //Backup của RecommendServices.GetSceneRecommend với scene "related"
        public async Task<List<Product>> GetRelatedProductsAsync(string productSysId, int topN = 10)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.ProductSysId == productSysId);
            if (product == null || product.Category == null)
            {
                return new List<Product>();
            }
            return await _context.Products
                .Where(p =>
                    p.ProductSysId != productSysId &&
                    p.CategoryId == product.CategoryId &&
                    p.Visible == true &&
                    p.Stock >= 1 &&
                    p.Status != "outstock"
                )
                .OrderByDescending(p => p.OrderItems.Count)
                .Take(topN)
                .ToListAsync();
        }

        public static string GenerateProductSysId()
        {
            return "prd_" + Guid.NewGuid()
                .ToString("N")
                .Substring(0, 10);
        }


    }
}
