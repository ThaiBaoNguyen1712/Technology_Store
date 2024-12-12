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

            var cart_items_count = 0;
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                cart_items_count = _context.CartItems.Where(x=>x.Cart.UserId == int.Parse(userId)).Count();
            }
  

            ViewBag.Categories = categories;
            ViewBag.phone = smartphone_products;
            ViewBag.laptop = laptop_products;
            ViewBag.tablet = tablet_products;
            ViewBag.watch = watch_products;
            ViewBag.accessory = accessory_products;
            ViewBag.cart_numb = cart_items_count;
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
        protected virtual void SiteInfor()
        {
            var settings = _context.Settings.ToList();

            // Gán giá trị vào ViewBag
            ViewBag.LogoUrl = settings.FirstOrDefault(s => s.Key == "LogoUrl")?.Value ?? "";
            ViewBag.NameWebsite = settings.FirstOrDefault(s => s.Key == "NameWebsite")?.Value ?? "";
            ViewBag.Slogan = settings.FirstOrDefault(s => s.Key == "Slogan")?.Value ?? "";
            ViewBag.Description = settings.FirstOrDefault(s => s.Key == "Description")?.Value ?? "";
            ViewBag.FacebookUrl = settings.FirstOrDefault(s => s.Key == "FacebookUrl")?.Value ?? "";
            ViewBag.InstagramUrl = settings.FirstOrDefault(s => s.Key == "InstagramUrl")?.Value ?? "";
            ViewBag.TwitterUrl = settings.FirstOrDefault(s => s.Key == "TwitterUrl")?.Value ?? "";
            ViewBag.YoutubeUrl = settings.FirstOrDefault(s => s.Key == "YoutubeUrl")?.Value ?? "";
            ViewBag.NameCompany = settings.FirstOrDefault(s => s.Key == "NameCompany")?.Value ?? "";
            ViewBag.PhoneNumber = settings.FirstOrDefault(s => s.Key == "PhoneNumber")?.Value ?? "";
            ViewBag.Email = settings.FirstOrDefault(s => s.Key == "Email")?.Value ?? "";
            ViewBag.Address = settings.FirstOrDefault(s => s.Key == "Address")?.Value ?? "";
            ViewBag.MoreInfo = settings.FirstOrDefault(s => s.Key == "MoreInfo")?.Value ?? "";
        }

    }
}
