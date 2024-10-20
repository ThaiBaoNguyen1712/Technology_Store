using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tech_Store.Models;

namespace Tech_Store.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, ApplicationDbContext context)
        {
            _logger = logger;
            _configuration = configuration;
            _context = context;
        }

        public IActionResult Index()
        {
            //Cate ID 
            // 1 Điện Thoại, 2 Laptop, 3 Máy tính Bảng ,4 Đồng hồ điện tử, 5 phụ kiện
            var categories = _context.Categories.Where(x=>x.Visible == 1).ToList();
            var smartphone_products = _context.Products.Where(x => x.CategoryId == 1).OrderByDescending(x => x.CreatedAt).Take(10).ToList(); ;
            var laptop_products = _context.Products.Where(x => x.CategoryId == 2).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
			var tablet_products = _context.Products.Where(x => x.CategoryId == 3).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
			var watch_products = _context.Products.Where(x => x.CategoryId == 4).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
			var accessory_products = _context.Products.Where(x => x.CategoryId == 5).OrderByDescending(x => x.CreatedAt).Take(10).ToList();

   //         var hot_smartphones = _context.Products.Where(_x => _x.CategoryId == 1).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_laptop = _context.Products.Where(_x => _x.CategoryId == 2).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_tablet = _context.Products.Where(_x => _x.CategoryId == 3).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_watch = _context.Products.Where(_x => _x.CategoryId == 4).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_accessory = _context.Products.Where(_x => _x.CategoryId == 5).OrderBy(x => x.OrderItems).Take(10).ToList();

			ViewBag.Categories = categories;
            ViewBag.phone = smartphone_products;
            ViewBag.laptop = laptop_products;
            ViewBag.tablet = tablet_products;
            ViewBag.watch = watch_products;
            //ViewBag.hot_phone = hot_smartphones;
            //ViewBag.hot_laptop = hot_laptop;
            //ViewBag.hot_tablet = hot_tablet;
            //ViewBag.hot_watch = hot_watch;
            //ViewBag.hot_accessory = hot_accessory;
            ViewBag.accessory = accessory_products;

            return View();
        }
        [Route("View/{id}")]
        public IActionResult View(int id)
        {
			var categories = _context.Categories.Where(x => x.Visible == 1).ToList();
			var smartphone_products = _context.Products.Where(x => x.CategoryId == 1).OrderByDescending(x => x.CreatedAt).Take(10).ToList(); ;
			var laptop_products = _context.Products.Where(x => x.CategoryId == 2).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
			var tablet_products = _context.Products.Where(x => x.CategoryId == 3).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
			var watch_products = _context.Products.Where(x => x.CategoryId == 4).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
			var accessory_products = _context.Products.Where(x => x.CategoryId == 5).OrderByDescending(x => x.CreatedAt).Take(10).ToList();

			//         var hot_smartphones = _context.Products.Where(_x => _x.CategoryId == 1).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_laptop = _context.Products.Where(_x => _x.CategoryId == 2).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_tablet = _context.Products.Where(_x => _x.CategoryId == 3).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_watch = _context.Products.Where(_x => _x.CategoryId == 4).OrderBy(x => x.OrderItems).Take(10).ToList();
			//var hot_accessory = _context.Products.Where(_x => _x.CategoryId == 5).OrderBy(x => x.OrderItems).Take(10).ToList();

			ViewBag.Categories = categories;
			ViewBag.phone = smartphone_products;
			ViewBag.laptop = laptop_products;
			ViewBag.tablet = tablet_products;
			ViewBag.watch = watch_products;
			var product = _context.Products.Include(x=>x.VarientProducts).Include(x=>x.Galleries)
                .Include(x=>x.Reviews).FirstOrDefault(x=>x.ProductId == id);

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
