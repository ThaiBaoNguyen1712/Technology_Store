using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using Tech_Store.Extensions;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services;
using Tech_Store.Services.Client;
using Tech_Store.Services.Client.Storefront;
using X.PagedList.Extensions;

namespace Tech_Store.Controllers
{
    public class HomeController : BaseController
    {
        private readonly RedisService _redis;
        private readonly IBannerQueryService _bannerQueryService;
        private readonly IHomePageContentService _homePageContentService;

        public HomeController(
            ApplicationDbContext context,
            RedisService redis,
            IBannerQueryService bannerQueryService,
            IHomePageContentService homePageContentService)
			: base(context) 
		{
            _redis = redis;
            _bannerQueryService = bannerQueryService;
            _homePageContentService = homePageContentService;
        }

        public async Task<IActionResult> Index()
        {
            var content = await _homePageContentService.GetHomePageContentAsync();

            ViewBag.productsHotSale = content.HotSaleProducts;
            ViewBag.List_cate = content.Categories;
            ViewBag.ProductsByCategory = content.ProductsByCategory;
            ViewBag.List_TopHotSearch = content.TopHotSearchByCategory;
            ViewBag.HotSearchByCategory = content.HotSearchByCategory;
            ViewBag.BrandsByCategory = content.BrandsByCategory;
            ViewBag.HomeHeroMainBanners = content.HomeHeroMainBanners;
            ViewBag.HomeHeroPromoBanners = content.HomeHeroPromoBanners;

            return View();
        }


        [OutputCache(Duration = 60, VaryByRouteValueNames = new[] { "slug" })]
        [Route("View/{slug}")]
        public async Task<IActionResult> View(string slug)
        {
            // Load product and related data in a single query
            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.VarientProducts)
                    .ThenInclude(vp => vp.VariantAttributes)
                        .ThenInclude(va => va.AttributeValue)
                .Include(p => p.Galleries)
                .Include(p => p.Reviews)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Slug == slug);

              if (product == null) return NotFound();

              HttpContext.Items["product_id"] = product.ProductId;
              HttpContext.Items["product_sys_id"] = product.ProductSysId ?? string.Empty;

            // Load specs in a single query
            var specs = await _context.Species
                .AsNoTracking()
                .Where(s => s.IsActive && s.IsVisibleOnProductPage)
                .Include(s => s.SpecValues.Where(v => v.ProductId == product.ProductId))
                .Where(s => s.SpecValues.Any(v => v.ProductId == product.ProductId))
                .OrderBy(s => s.GroupName)
                .ThenBy(s => s.SortOrder)
                .ToListAsync();

            // Load reviews and calculate summary
            var reviews = await _context.Reviews
                .AsNoTracking()
                .Include(r => r.User)
                .Where(r => r.ProductId == product.ProductId)
                .ToListAsync();

            var reviewCount = reviews.Count;
            var ratingSummary = reviews.GroupBy(r => r.Rating)
                .Select(g => new { Rating = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Rating, x => x.Count);
            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            // Load comments and build comment tree
            var comments = await _context.Comments
                .AsNoTracking()
                .Include(c => c.User)
                .Where(c => c.ProductId == product.ProductId && c.Status == true)
                .ToListAsync();

            var commentTree = comments
                .Where(c => c.ParentCommentId == null)
                .Select(c => new CommentVM
                {
                    CommentId = c.CommentId,
                    ProductId = c.ProductId,
                    UserId = c.UserId,
                    Content = c.Content,
                    ParentCommentId = c.ParentCommentId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    Status = c.Status,
                    UserName = $"{c.User.LastName} {c.User.FirstName}",
                    UserAvatar = c.User.Img,
                    Replies = comments
                        .Where(r => r.ParentCommentId == c.CommentId)
                        .OrderBy(r => r.CreatedAt)
                        .Select(r => new CommentVM
                        {
                            CommentId = r.CommentId,
                            ProductId = r.ProductId,
                            UserId = r.UserId,
                            Content = r.Content,
                            ParentCommentId = r.ParentCommentId,
                            CreatedAt = r.CreatedAt,
                            UpdatedAt = r.UpdatedAt,
                            Status = r.Status,
                            UserName = $"{r.User.LastName} {r.User.FirstName}",
                            UserAvatar = r.User.Img
                        })
                        .ToList()
                })
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            // Pass data to the view
            ViewBag.specs = specs;
            ViewBag.review_count = reviewCount;
            ViewBag.reviews = reviews;
            ViewBag.ratingSummary = ratingSummary;
            ViewBag.averageRating = averageRating;
            ViewBag.comments = commentTree;


            return View(product);
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet("Category/{eng_title}")]
        public async Task<IActionResult> Category(string eng_title, string? order,string? price,string? brand,int page = 1)
        {
            int pageSize = 20;

            var cate = _context.Categories
                .AsNoTracking()
                .FirstOrDefault(x => x.EngTitle == eng_title);

            if (cate == null)
                return NotFound();

            IQueryable<Models.Product> query = _context.Products
                .AsNoTracking()
                .Where(x =>
                    x.CategoryId == cate.CategoryId &&
                    x.Status != "outstock")
                .WhereStorefrontAvailable();

            //  Brand filter
            if (!string.IsNullOrEmpty(brand) && int.TryParse(brand, out int brandId))
                query = query.Where(x => x.BrandId == brandId);

            // Price filter
            query = query
                .ApplyStorefrontPriceFilter(price)
                .ApplyStorefrontSort(order);

            var products = query.ToPagedList(page, pageSize);

            // 🔥 Hot search
            var list_HotSearch = await _redis.GetTopSearchKeywordsByCategory(cate.EngTitle, 10);

            ViewBag.list_HotSearch = list_HotSearch;

            ViewBag.Order = order;
            ViewBag.Price = price;
            ViewBag.Brand = brand;
            ViewBag.Category = cate;
            ViewBag.CategoryHeroBanners = await _bannerQueryService.GetBannersAsync("category-hero", cate.CategoryId, null, 1);

            ViewBag.list_brand = _context.Brands
                .AsNoTracking()
                .Where(b =>
                    !b.BrandCategories.Any() ||
                    b.BrandCategories.Any(bc => bc.CategoryId == cate.CategoryId))
                .ApplyRandomizedBrandOrdering()
                .ToList();

            return View(products);
        }

        [HttpGet("Category")]
        public async Task<IActionResult> Categories()
        {
            var content = await _homePageContentService.GetCategoryLandingContentAsync();

            ViewBag.CategoryCards = content.CategoryCards;
            ViewBag.CategoryProductCounts = content.CategoryProductCounts;
            ViewBag.FeaturedProducts = content.FeaturedProducts;

            return View();
        }

        [HttpGet("Brand/{brand_name}")]
        public async Task<IActionResult> Brand(string brand_name,string? key, string? order,string? price,int page = 1)
        {
            int pageSize = 20;

            var currentBrand = _context.Brands
                .Include(b => b.Category)
                .Include(b => b.BrandCategories)
                .FirstOrDefault(b => b.Name.Contains(brand_name));

            if (currentBrand == null)
                return NotFound();

            IQueryable<Models.Product> query = _context.Products
                .AsNoTracking()
                .Where(p => p.BrandId == currentBrand.BrandId)
                .WhereStorefrontAvailable()
                .ApplyStorefrontKeywordSearch(key)
                .ApplyStorefrontPriceFilter(price)
                .ApplyStorefrontSort(order);

            var products = query.ToPagedList(page, pageSize);

            // Hot search
            if (!string.IsNullOrWhiteSpace(key))
            {
                await _redis.IncrementHotKeywordAsync(
                    key,
                    new[] { currentBrand.Category?.EngTitle ?? "all" });
            }

            ViewBag.Brand = currentBrand;
            ViewBag.Category = currentBrand.Category;
            ViewBag.Order = order;
            ViewBag.Price = price;
            ViewBag.Key = key;
            ViewBag.BrandHeroBanners = await _bannerQueryService.GetBannersAsync("brand-hero", currentBrand.CategoryId, currentBrand.BrandId, 1);

            ViewBag.list_brand = _context.Brands
                .Where(b =>
                    !b.BrandCategories.Any() ||
                    (currentBrand.CategoryId != null && b.BrandCategories.Any(bc => bc.CategoryId == currentBrand.CategoryId)))
                .ApplyRandomizedBrandOrdering()
                .ToList();

            return View(products);
        }

        [HttpGet("Search/{key?}")]
        public async Task<IActionResult> SearchAsync(string? key, string? category, string? brand, string? order, string? price, int page = 1)
        {
            int pageSize = 20;

            IQueryable<Models.Product> query = _context.Products
                .AsNoTracking()
                .WhereStorefrontAvailable()
                .ApplyStorefrontKeywordSearch(key);

            // 2️⃣ Category
            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int cateId))
                query = query.Where(x => x.CategoryId == cateId);

            // 3️⃣ Brand
            if (!string.IsNullOrEmpty(brand) && int.TryParse(brand, out int brandId))
                query = query.Where(x => x.BrandId == brandId);

            query = query
                .ApplyStorefrontPriceFilter(price)
                .ApplyStorefrontSort(order);

            var pagedProducts = query
                .Select(p => new Models.Product
                {
                    ProductId = p.ProductId,
                    Slug = p.Slug,
                    Image = p.Image,
                    Name = p.Name,
                    SellPrice = p.SellPrice,
                    OriginalPrice = p.OriginalPrice,
                    DiscountPercentage = p.DiscountPercentage,
                    DiscountAmount = p.DiscountAmount,
                    Category = new Category
                    {
                        EngTitle = p.Category.EngTitle
                    }
                })
                .ToPagedList(page, pageSize);

            // 🔥 Hot search
            if (!string.IsNullOrWhiteSpace(key))
            {
                var matchedCategorySlugs = pagedProducts
                    .Select(x => x.Category?.EngTitle)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct()
                    .Take(3)
                    .Cast<string>()
                    .ToList();

                if (!string.IsNullOrWhiteSpace(category) && int.TryParse(category, out var selectedCategoryId))
                {
                    var selectedCategorySlug = await _context.Categories
                        .AsNoTracking()
                        .Where(x => x.CategoryId == selectedCategoryId)
                        .Select(x => x.EngTitle)
                        .FirstOrDefaultAsync();

                    if (!string.IsNullOrWhiteSpace(selectedCategorySlug))
                    {
                        matchedCategorySlugs.Insert(0, selectedCategorySlug);
                    }
                }

                await _redis.IncrementHotKeywordAsync(key, matchedCategorySlugs);
            }

            ViewBag.Order = order;
            ViewBag.Price = price;
            ViewBag.Brand = brand;
            ViewBag.Category = category;
            ViewBag.Key = key;

            ViewBag.list_brand = _context.Brands
                .AsNoTracking()
                .ApplyRandomizedBrandOrdering()
                .Select(b => new { b.BrandId, b.Name })
                .ToList();

            ViewBag.list_category = _context.Categories
                .AsNoTracking()
                .Where(c => c.VisibleOnOtherPages == 1)
                .Select(c => new { c.CategoryId, c.Name })
                .ToList();

            return View(pagedProducts);
        }

    }
}
