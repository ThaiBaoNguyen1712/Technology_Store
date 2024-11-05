using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Models;

namespace Tech_Store.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly ApplicationDbContext _context;

        public BaseController(ApplicationDbContext context)
        {
            _context = context;
        }
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Load common data for all actions
            LoadUser();
            LoadProductsAndCategories();
        }
        protected virtual void LoadProductsAndCategories()
        {
            var categories = _context.Categories.Where(x => x.Visible == 1).ToList();
            var smartphone_products = _context.Products.Where(x => x.CategoryId == 1).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var laptop_products = _context.Products.Where(x => x.CategoryId == 2).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var tablet_products = _context.Products.Where(x => x.CategoryId == 3).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var watch_products = _context.Products.Where(x => x.CategoryId == 4).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var accessory_products = _context.Products.Where(x => x.CategoryId == 5).OrderByDescending(x => x.CreatedAt).Take(10).ToList();

            //var hot_smartphones = _context.Products.Where(_x => _x.CategoryId == 1).OrderBy(x => x.OrderItems).Take(10).ToList();
            //var hot_laptop = _context.Products.Where(_x => _x.CategoryId == 2).OrderBy(x => x.OrderItems).Take(10).ToList();
            //var hot_tablet = _context.Products.Where(_x => _x.CategoryId == 3).OrderBy(x => x.OrderItems).Take(10).ToList();
            //var hot_watch = _context.Products.Where(_x => _x.CategoryId == 4).OrderBy(x => x.OrderItems).Take(10).ToList();
            //var hot_accessory = _context.Products.Where(_x => _x.CategoryId == 5).OrderBy(x => x.OrderItems).Take(10).ToList();

            //ViewBag.hot_phone = hot_smartphones;
            //ViewBag.hot_laptop = hot_laptop;
            //ViewBag.hot_tablet = hot_tablet;
            //ViewBag.hot_watch = hot_watch;
            //ViewBag.hot_accessory = hot_accessory;

            ViewBag.Categories = categories;
            ViewBag.phone = smartphone_products;
            ViewBag.laptop = laptop_products;
            ViewBag.tablet = tablet_products;
            ViewBag.watch = watch_products;
            ViewBag.accessory = accessory_products;
        }
 
        protected virtual void LoadUser()
        {
            if (!User.Identity.IsAuthenticated)
            {
                ViewBag.User = null;
                return;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.User = null;
                return;
            }

            var user = _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefault(u => u.UserId == int.Parse(userId));

            ViewBag.User = user;
        }
    }
}
