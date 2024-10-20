using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class POSController : Controller
    {
        private readonly ApplicationDbContext _context;
        public POSController(ApplicationDbContext context)
        {
            _context = context;
        }
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            var products = _context.Products.OrderByDescending(x => x.ProductId).ToList();
            var users = _context.Users.OrderByDescending(x => x.UserId).ToList();

            ViewBag.category = categories;
            ViewBag.product = products;
            ViewBag.user = users;
            return View();
        }

        [HttpGet]
        [Route("GetUser")]
        public async Task<JsonResult> GetUser(int id)
        {
            var cus = await _context.Users.Include(x => x.Addresses).Select(x=> new {x.UserId,x.LastName,x.FirstName,x.PhoneNumber,x.Addresses})
                .FirstOrDefaultAsync(x => x.UserId == id);
            if (cus == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng này" });
            }
            return Json(cus);
        }
        [HttpGet]
        [Route("CheckVoucher")]
        public async Task<JsonResult> CheckVoucher(string code)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(x => x.Code.ToLower().Trim() == code.ToLower().Trim());

            if (voucher == null)
            {
                return Json(new { success = false, message = "Voucher không đúng" });
            }
            else
            {
                // Chuyển DateTime.Now thành DateOnly
                var today = DateOnly.FromDateTime(DateTime.Now);

                // Kiểm tra nếu voucher chưa bắt đầu
                if (voucher.StartedAt.HasValue && voucher.StartedAt.Value > today)
                {
                    return Json(new { success = false, message = "Voucher chưa đến thời hạn sử dụng" });
                }

                // Kiểm tra nếu voucher đã hết hạn
                if (voucher.ExpiredAt.HasValue && voucher.ExpiredAt.Value < today)
                {
                    return Json(new { success = false, message = "Voucher đã hết hạn" });
                }
                //Kiểm tra nếu SL voucher đã hết 
                if(voucher.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Voucher đã hết" });
                }
            }

            return Json(new { success = true, voucher });
        }

        [HttpGet]
        [Route("GetProduct")]
        public async Task<JsonResult> GetProduct(int id)
        {
            var product = await _context.Products.Include(x=>x.VarientProducts).Include(x=>x.Brand).Include(x=>x.Category)
                .Select(x => new
                {
                    x.ProductId,
                    x.Name,
                    SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                    x.Description,
                    x.Image,
                    x.Stock,
                    x.Sku,
                    x.VarientProducts,
                    x.Brand,
                    x.Category
                })
                .FirstOrDefaultAsync(x => x.ProductId == id);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không hợp lệ" });
            }

            // Chỉ trả về những thông tin cần thiết
            return Json(new
            {
                success = true,
                product
            });
        }
        [HttpGet]
        [Route("GetProductsByCategory")]
        public async Task<JsonResult> GetProductsByCategory(int cateId)
        {
            // Nếu cateId == null, lấy tất cả sản phẩm, ngược lại chỉ lấy sản phẩm thuộc cateId
            var products = await _context.Products
                .Select(x => new
                {
                    x.ProductId,
                    x.Name,
                    SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                    x.Description,
                    x.Image
                })
               .ToListAsync();
            if(cateId != 0)
            {
                products = await _context.Products
                  .Where(x => x.CategoryId == cateId)// Nếu cateId không có giá trị, lấy tất cả sản phẩm
               .Select(x => new
               {
                   x.ProductId,
                   x.Name,
                   SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                   x.Description,
                   x.Image
               })
               .ToListAsync();
            }
            // Kiểm tra xem có sản phẩm nào không
            if (products == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm phù hợp" });
            }

            return Json(new
            {
                success = true,
                products
            });
        }
        [HttpGet]
        [Route("GetProducts")]
        public async Task<JsonResult> GetProducts(string name, int cateId)
        {
            IQueryable<Product> query = _context.Products;

            // Apply category filter if cateId is provided
            if (cateId > 0)
            {
                query = query.Where(x => x.CategoryId == cateId);
            }

            // Apply name filter if name is provided
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(x => x.Name.Contains(name));
            }

            var products = await query.Select(x => new
            {
                x.ProductId,
                x.Name,
                SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                x.Description,
                x.Image
            }).OrderByDescending(x => x.ProductId).ToListAsync();

            return Json(new
            {
                success = true,
                products
            });
        }
        [HttpGet]
        [Route("GetVarientProduct")]
        public async Task<JsonResult> GetVarientProduct(int id,int productId)
        {
            var varient = _context.VarientProducts.Select(x => new { x.VarientId,x.Sku,x.Attributes,x.Price,x.Stock,x.Product}).
                FirstOrDefaultAsync(x=>x.VarientId == id && x.Product.ProductId == productId);
            if(varient == null)
            {
                return Json(new { success = false, message = "Không tìm thấy biến thể" });
            }
            return Json(new { success = true, varient });
        }

    }
}
