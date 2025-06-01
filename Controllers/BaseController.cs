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
            SiteInfor();
            if (User.Identity.IsAuthenticated)
            {
                GetUserNotifications();
                ViewBag.UserInfoRequire = CheckUserInfoRequire();
            }
        }
        protected virtual void LoadProductsAndCategories()
        {
            var categories = _context.Categories.Where(x => x.Visible == 1).ToList();
            var smartphone_products = _context.Products.Where(x => x.CategoryId == 1).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var laptop_products = _context.Products.Where(x => x.CategoryId == 2).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var tablet_products = _context.Products.Where(x => x.CategoryId == 3).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var watch_products = _context.Products.Where(x => x.CategoryId == 4).OrderByDescending(x => x.CreatedAt).Take(10).ToList();
            var accessory_products = _context.Products.Where(x => x.CategoryId == 5).OrderByDescending(x => x.CreatedAt).Take(10).ToList();

            //Cart_item count
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
                .Include(r => r.Roles)
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
            ViewBag.AddressCompany = settings.FirstOrDefault(s => s.Key == "Address")?.Value ?? "";
            ViewBag.MoreInfo = settings.FirstOrDefault(s => s.Key == "MoreInfo")?.Value ?? "";
        }

        protected virtual void GetUserNotifications()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notifications = _context.UserNotifications
                .Where(un => un.UserId == userId)
                .Include(un => un.Notification)
                .OrderByDescending(un => un.Notification.CreatedAt)
                .Select(un => new
                {
                    un.UserNotificationId,
                    un.UserId,
                    un.Notification.Title,
                    un.Notification.Message,
                    un.Notification.RedirectUrl,
                    un.Notification.Type,
                    un.Notification.CreatedAt,
                    un.IsRead
                })
                .Take(20)
                .ToList();

            ViewBag.Notifications = notifications;
            ViewBag.Notification_count = notifications.Where(x => x.IsRead == false).Count();
        }

        protected bool CheckUserInfoRequire()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var user = _context.Users.FirstOrDefault(x=>x.UserId == userId);  
            if(user == null)
            {
                return false;
            }
            if (user.Addresses == null || !user.Addresses.Any() || user.Addresses.All(a => string.IsNullOrWhiteSpace(a.Ward))
                || user.Addresses.All(a => string.IsNullOrWhiteSpace(a.Province)) || user.Addresses.All(a => string.IsNullOrWhiteSpace(a.Province))
                || user.Addresses.All(a=>string.IsNullOrEmpty(a.District) || user.Addresses.All(a=>string.IsNullOrEmpty(a.AddressLine))))
            {
                return false;
            }
            if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName) || string.IsNullOrEmpty(user.PhoneNumber) || user.Addresses.Any() == null)
            {
                return false;
            }
            return true;
        }
    }
}
