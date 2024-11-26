using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Diagnostics;
using System.Drawing.Printing;
using Tech_Store.Models;
using X.PagedList.Extensions;

namespace Tech_Store.Controllers
{
    public class HomeController : BaseController
    {
		private readonly ILogger<HomeController> _logger;
		private readonly IConfiguration _configuration;

		// Khai báo chỉ cần ILogger và IConfiguration
		public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ApplicationDbContext context)
			: base(context) // Gọi constructor của BaseController
		{
			_logger = logger;
			_configuration = configuration;
		}

        public IActionResult Index()
        {
            // Lấy danh sách danh mục có trạng thái hiển thị
            var list_cate = _context.Categories
                .Where(x => x.Visible == 1)
                .ToList();

            // Lấy tối đa 10 sản phẩm cho mỗi danh mục
            var productsByCategory = list_cate.Select(category => new
            {
                Category = category,
                Products = _context.Products
                    .Where(p => p.CategoryId == category.CategoryId)
                    .Take(10)
                    .ToList()
            }).ToList();

            // Lấy danh sách sản phẩm Hot Sale
            var hotSaleProducts = _context.Products
                .Where(x => x.Visible == true)
                .OrderByDescending(x => x.OrderItems.Count)
                .Take(10)
                .ToList();

            // Nếu số lượng sản phẩm Hot Sale ít hơn 10, bổ sung thêm sản phẩm ngẫu nhiên
            if (hotSaleProducts.Count < 10)
            {
                int remainingSlots = 10 - hotSaleProducts.Count;

                // Lấy danh sách các sản phẩm chưa có trong hotSaleProducts
                var additionalProducts = _context.Products
                    .Where(x => x.Visible == true && !hotSaleProducts.Select(h => h.ProductId).Contains(x.ProductId))
                    .OrderBy(r => EF.Functions.Random()) // Sử dụng EF.Functions.Random() thay vì Guid.NewGuid()
                    .Take(remainingSlots)
                    .ToList();

                // Gộp danh sách sản phẩm Hot Sale với các sản phẩm ngẫu nhiên
                hotSaleProducts.AddRange(additionalProducts);
            }

            // Sử dụng ViewBag để truyền dữ liệu vào View
            ViewBag.productsHotSale = hotSaleProducts;
            ViewBag.List_cate = list_cate;
            ViewBag.ProductsByCategory = productsByCategory;
            return View();
        }


        [Route("View/{slug}")]
        public IActionResult View(string slug)
        {
            var product = _context.Products
                .Include(x => x.VarientProducts)
                .Include(x => x.Galleries)
                .Include(x => x.Reviews)
                .FirstOrDefault(x => x.Slug.Contains(slug));

            if (product == null)
            {
                return NotFound();
            }

            var reviews = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.Product.Slug.Contains(slug))
                .AsNoTracking()
                .ToList();

            var review_count = reviews.Count();
            var ratingSummary = reviews
                .GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Rating, x => x.Count);

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

         

            ViewBag.review_count = review_count;
            ViewBag.reviews = reviews;
            ViewBag.ratingSummary = ratingSummary;
            ViewBag.averageRating = averageRating;

            ViewBag.related_products = _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Take(10)
                .AsNoTracking()
                .ToList();

            return View(product);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [Route("Category/{eng_title}")]
        public IActionResult Category(string eng_title,int? page)
        {
            int pageSize = 20; 
            int pageNumber = page ?? 1;

            var products = _context.Products.Where(x => x.Category.EngTitle.Contains(eng_title)).OrderByDescending(x=>x.ProductId).ToPagedList(pageNumber, pageSize); // Áp dụng phân trang;
            var cate = _context.Categories.FirstOrDefault(x => x.EngTitle.Contains(eng_title));
            var list_brand = _context.Brands.ToList();
            ViewBag.list_brand = list_brand;
            ViewBag.Category = cate;
            return View(products);
        }

        [HttpGet("Category/FilterCategory")]
        public IActionResult FilterCategory(string eng_title, string? order, string? price, string? brand, int pageNumber = 1)
        {
            int pageSize = 20;

            // Truy vấn cơ bản
            IQueryable<Models.Product> query = _context.Products
                .Where(x => x.Category.EngTitle.Contains(eng_title));

            // Sắp xếp theo `order`
            if (!string.IsNullOrEmpty(order))
            {
                switch (order.ToLower())
                {
                    case "alphabet":
                        query = query.OrderBy(x => x.Name);
                        break;
                    case "alphabet_desc":
                        query = query.OrderByDescending(x => x.Name);
                        break;
                    case "price":
                        query = query.OrderBy(x => x.SellPrice);
                        break;
                    case "price_desc":
                        query = query.OrderByDescending(x => x.SellPrice);
                        break;
                    case "care":
                        query = query.OrderBy(x => x.Reviews);
                        break;
                    default:
                        query = query.OrderByDescending(x => x.ProductId); // Sắp xếp mặc định
                        break;
                }
            }

            // Lọc theo `price`
            if (!string.IsNullOrEmpty(price))
            {
                switch (price.ToLower())
                {
                    case "max5":
                        query = query.Where(x => x.SellPrice.HasValue && x.SellPrice.Value > 0 && x.SellPrice.Value < 5000000);
                        break;
                    case "max10":
                        query = query.Where(x => x.SellPrice.HasValue && x.SellPrice.Value >= 5000000 && x.SellPrice.Value < 10000000);
                        break;
                    case "max20":
                        query = query.Where(x => x.SellPrice.HasValue && x.SellPrice.Value >= 10000000 && x.SellPrice.Value < 20000000);
                        break;
                    case "max50":
                        query = query.Where(x => x.SellPrice.HasValue && x.SellPrice.Value >= 20000000 && x.SellPrice.Value < 50000000);
                        break;
                    case "more":
                        query = query.Where(x => x.SellPrice.HasValue && x.SellPrice.Value >= 50000000);
                        break;
                }
            }

            // Lọc theo `brandId`
            if (!string.IsNullOrEmpty(brand) && int.TryParse(brand, out int brandIdValue))
            {
                query = query.Where(x => x.BrandId == brandIdValue);
            }

            // Phân trang
            var products = query.ToPagedList(pageNumber, pageSize);
            ViewBag.Order = order;
            ViewBag.Price = price;
            ViewBag.Brand = brand;
            var cate = _context.Categories.FirstOrDefault(x => x.EngTitle.Contains(eng_title));
            var list_brand = _context.Brands.ToList();
            ViewBag.list_brand = list_brand;
            ViewBag.Category = cate;
  
            // Trả về view
            return View("Category", products);
        }

    }
}
