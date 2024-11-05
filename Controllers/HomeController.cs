using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tech_Store.Models;

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
                    .Take(10) // Lấy 10 sản phẩm đầu tiên của danh mục
                    .ToList()
            }).ToList();

            // Sử dụng ViewBag để truyền dữ liệu vào View
            ViewBag.List_cate = list_cate;
            ViewBag.ProductsByCategory = productsByCategory;
            return View();
        }

        [Route("View/{id}")]
        public IActionResult View(int id)
        {
			LoadProductsAndCategories(); // Gọi phương thức từ BaseController

			var product = _context.Products
				.Include(x => x.VarientProducts)
				.Include(x => x.Galleries)
				.Include(x => x.Reviews)
				.FirstOrDefault(x => x.ProductId == id);

			var related_products = _context.Products.Where(x => x.CategoryId == product.CategoryId).Take(10).ToList();
            ViewBag.related_products = related_products;
			return View(product);
		}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
