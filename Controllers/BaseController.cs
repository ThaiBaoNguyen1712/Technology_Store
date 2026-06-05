using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using Tech_Store.Models;

namespace Tech_Store.Controllers
{
    public abstract class BaseController : Controller
    {
        private static readonly TimeSpan SharedLayoutCacheDuration = TimeSpan.FromMinutes(5);
        private const string HeaderNavigationCacheKey = "layout:header-navigation";
        private const string SiteInfoCacheKey = "layout:site-info";
        private static readonly SemaphoreSlim HeaderNavigationCacheLock = new(1, 1);
        private static readonly SemaphoreSlim SiteInfoCacheLock = new(1, 1);

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
            if (User.Identity?.IsAuthenticated == true)
            {
                GetUserNotifications();
                ViewBag.UserInfoRequire = CheckUserInfoRequire();
            }
        }
        protected virtual void LoadProductsAndCategories()
        {
            var memoryCache = HttpContext.RequestServices.GetService<IMemoryCache>();
            if (memoryCache == null || !memoryCache.TryGetValue(HeaderNavigationCacheKey, out HeaderNavigationData? navigationData))
            {
                HeaderNavigationCacheLock.Wait();
                try
                {
                    if (memoryCache == null || !memoryCache.TryGetValue(HeaderNavigationCacheKey, out navigationData))
                    {
                        navigationData = new HeaderNavigationData
                        {
                            Categories = _context.Categories
                                .AsNoTracking()
                                .Where(x => x.Visible == 1)
                                .OrderBy(x => x.SortOrder)
                                .ThenBy(x => x.Name)
                                .ToList(),
                            PhoneProducts = GetLatestProductsByCategory(1),
                            LaptopProducts = GetLatestProductsByCategory(2),
                            TabletProducts = GetLatestProductsByCategory(3),
                            WatchProducts = GetLatestProductsByCategory(4),
                            AccessoryProducts = GetLatestProductsByCategory(5)
                        };

                        memoryCache?.Set(HeaderNavigationCacheKey, navigationData, SharedLayoutCacheDuration);
                    }
                }
                finally
                {
                    HeaderNavigationCacheLock.Release();
                }
            }

            //Cart_item count
            var cart_items_count = 0;
            var wishlistProductIds = new List<int>();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userId, out var parsedUserId))
            {
                cart_items_count = _context.CartItems
                    .AsNoTracking()
                    .Count(x=>x.Cart.UserId == parsedUserId);
                wishlistProductIds = _context.Wishlists
                    .AsNoTracking()
                    .Where(x => x.UserId == parsedUserId && x.ProductId.HasValue)
                    .Select(x => x.ProductId!.Value)
                    .ToList();
            }
            navigationData ??= new HeaderNavigationData();

        
            ViewBag.Categories = navigationData.Categories;
            ViewBag.phone = navigationData.PhoneProducts;
            ViewBag.laptop = navigationData.LaptopProducts;
            ViewBag.tablet = navigationData.TabletProducts;
            ViewBag.watch = navigationData.WatchProducts;
            ViewBag.accessory = navigationData.AccessoryProducts;
            ViewBag.cart_numb = cart_items_count;
            ViewBag.WishlistProductIds = wishlistProductIds;
      
        }

        protected virtual void LoadUser()
        {
            if (User.Identity?.IsAuthenticated != true)
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

            if (!int.TryParse(userId, out var parsedUserId))
            {
                ViewBag.User = null;
                return;
            }

            var user = _context.Users
                .AsNoTracking()
                .Include(r => r.Roles)
                .Include(u => u.Addresses)
                .FirstOrDefault(u => u.UserId == parsedUserId);

            ViewBag.User = user;
        }
        protected virtual void SiteInfor()
        {
            var memoryCache = HttpContext.RequestServices.GetService<IMemoryCache>();
            if (memoryCache == null || !memoryCache.TryGetValue(SiteInfoCacheKey, out Dictionary<string, string?>? settings))
            {
                SiteInfoCacheLock.Wait();
                try
                {
                    if (memoryCache == null || !memoryCache.TryGetValue(SiteInfoCacheKey, out settings))
                    {
                        settings = _context.Settings
                            .AsNoTracking()
                            .ToList()
                            .GroupBy(s => s.Key)
                            .ToDictionary(g => g.Key, g => g.FirstOrDefault()?.Value);

                        memoryCache?.Set(SiteInfoCacheKey, settings, SharedLayoutCacheDuration);
                    }
                }
                finally
                {
                    SiteInfoCacheLock.Release();
                }
            }
            settings ??= new Dictionary<string, string?>();

            // Gán giá trị vào ViewBag
            ViewBag.LogoUrl = GetSetting(settings, "LogoUrl");
            ViewBag.NameWebsite = GetSetting(settings, "NameWebsite");
            ViewBag.Slogan = GetSetting(settings, "Slogan");
            ViewBag.Description = GetSetting(settings, "Description");
            ViewBag.FacebookUrl = GetSetting(settings, "FacebookUrl");
            ViewBag.InstagramUrl = GetSetting(settings, "InstagramUrl");
            ViewBag.TwitterUrl = GetSetting(settings, "TwitterUrl");
            ViewBag.YoutubeUrl = GetSetting(settings, "YoutubeUrl");
            ViewBag.NameCompany = GetSetting(settings, "NameCompany");
            ViewBag.PhoneNumber = GetSetting(settings, "PhoneNumber");
            ViewBag.Email = GetSetting(settings, "Email");
            ViewBag.AddressCompany = GetSetting(settings, "Address");
            ViewBag.MoreInfo = GetSetting(settings, "MoreInfo");
        }

        protected virtual void GetUserNotifications()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                ViewBag.Notifications = new List<object>();
                ViewBag.Notification_count = 0;
                return;
            }

            var notifications = _context.UserNotifications
                .AsNoTracking()
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
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            {
                return false;
            }

            var user = _context.Users
                .AsNoTracking()
                .Include(x => x.Addresses)
                .FirstOrDefault(x=>x.UserId == userId);  
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
            if (string.IsNullOrEmpty(user.FirstName) || string.IsNullOrEmpty(user.LastName) || string.IsNullOrEmpty(user.PhoneNumber))
            {
                return false;
            }
            return true;
        }

        private List<Product> GetLatestProductsByCategory(int categoryId)
        {
            return _context.Products
                .AsNoTracking()
                .Where(x => x.CategoryId == categoryId)
                .OrderBy(x => x.SortOrder)
                .ThenByDescending(x => x.CreatedAt)
                .Take(10)
                .ToList();
        }

        private static string GetSetting(Dictionary<string, string?> settings, string key)
        {
            return settings.TryGetValue(key, out var value) ? value ?? "" : "";
        }

        private sealed class HeaderNavigationData
        {
            public List<Category> Categories { get; init; } = new();
            public List<Product> PhoneProducts { get; init; } = new();
            public List<Product> LaptopProducts { get; init; } = new();
            public List<Product> TabletProducts { get; init; } = new();
            public List<Product> WatchProducts { get; init; } = new();
            public List<Product> AccessoryProducts { get; init; } = new();
        }
    }
}
