using Microsoft.EntityFrameworkCore;
using Tech_Store.Extensions;
using Tech_Store.Models;
using Tech_Store.Services.Client;

namespace Tech_Store.Services.Client.Storefront
{
    public class HomePageContentService : IHomePageContentService
    {
        private readonly ApplicationDbContext _context;
        private readonly RedisService _redisService;
        private readonly IBannerQueryService _bannerQueryService;

        public HomePageContentService(
            ApplicationDbContext context,
            RedisService redisService,
            IBannerQueryService bannerQueryService)
        {
            _context = context;
            _redisService = redisService;
            _bannerQueryService = bannerQueryService;
        }

        public async Task<HomePageContentResult> GetHomePageContentAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Include(p => p.Products)
                .Where(x => x.Visible == 1)
                .ToListAsync();

            var topHotSearchByCategory = await _redisService.GetTopSearchKeywords(categories, 6);

            var hotSearchByCategory = new Dictionary<string, List<string>>();
            foreach (var category in categories)
            {
                hotSearchByCategory[category.EngTitle] = await _redisService.GetTopSearchKeywordsByCategory(category.EngTitle, 12);
            }

            var brandsByCategory = new Dictionary<int, List<Brand>>();
            foreach (var category in categories)
            {
                brandsByCategory[category.CategoryId] = await _context.Brands
                    .AsNoTracking()
                    .Where(b =>
                        !b.BrandCategories.Any() ||
                        b.BrandCategories.Any(bc => bc.CategoryId == category.CategoryId))
                    .ApplyRandomizedBrandOrdering()
                    .ToListAsync();
            }

            var productsByCategory = new List<HomeCategoryProductsSection>();
            foreach (var category in categories)
            {
                var products = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Brand)
                    .Where(p => p.CategoryId == category.CategoryId && p.Stock >= 1 && p.Status != "outstock")
                    .OrderByDescending(p => p.ProductId)
                    .Take(10)
                    .ToListAsync();

                productsByCategory.Add(new HomeCategoryProductsSection
                {
                    Category = category,
                    Products = products
                });
            }

            var hotSaleProducts = await _context.Products
                .AsNoTracking()
                .Where(x => x.Visible == true && x.Stock >= 1 && x.Status != "outstock")
                .OrderByDescending(x => x.OrderItems.Count)
                .Take(10)
                .ToListAsync();

            if (hotSaleProducts.Count < 10)
            {
                var remainingSlots = 10 - hotSaleProducts.Count;
                var hotSaleIds = hotSaleProducts.Select(h => h.ProductId).ToList();

                var additionalProducts = await _context.Products
                    .AsNoTracking()
                    .Where(x => x.Visible == true && !hotSaleIds.Contains(x.ProductId))
                    .OrderBy(_ => EF.Functions.Random())
                    .Take(remainingSlots)
                    .ToListAsync();

                hotSaleProducts.AddRange(additionalProducts);
            }

            return new HomePageContentResult
            {
                Categories = categories,
                TopHotSearchByCategory = topHotSearchByCategory,
                HotSearchByCategory = hotSearchByCategory,
                BrandsByCategory = brandsByCategory,
                ProductsByCategory = productsByCategory,
                HotSaleProducts = hotSaleProducts,
                HomeHeroMainBanners = await _bannerQueryService.GetBannersAsync("home-hero-main", null, null, 3),
                HomeHeroPromoBanners = await _bannerQueryService.GetBannersAsync("home-hero-promo", null, null, 2)
            };
        }

        public async Task<CategoryLandingContentResult> GetCategoryLandingContentAsync()
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .Where(x => x.VisibleOnCategoryPage == 1)
                .OrderBy(x => x.Name)
                .Select(x => new Category
                {
                    CategoryId = x.CategoryId,
                    Name = x.Name,
                    EngTitle = x.EngTitle,
                    Image = x.Image,
                    Description = x.Description,
                    Visible = x.Visible,
                    VisibleOnCategoryPage = x.VisibleOnCategoryPage,
                    VisibleOnOtherPages = x.VisibleOnOtherPages
                })
                .ToListAsync();

            var categoryProductCounts = await _context.Products
                .AsNoTracking()
                .Where(x => x.CategoryId != null && x.Status != "outstock" && x.SellPrice > 0)
                .GroupBy(x => x.CategoryId!.Value)
                .Select(x => new { CategoryId = x.Key, Count = x.Count() })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);

            var featuredProducts = await _context.Products
                .AsNoTracking()
                .Where(x => x.Visible == true && x.Stock >= 1 && x.Status != "outstock" && x.SellPrice > 0)
                .OrderByDescending(x => x.OrderItems.Count)
                .ThenByDescending(x => x.ProductId)
                .Take(8)
                .ToListAsync();

            return new CategoryLandingContentResult
            {
                CategoryCards = categories,
                CategoryProductCounts = categoryProductCounts,
                FeaturedProducts = featuredProducts
            };
        }
    }
}
