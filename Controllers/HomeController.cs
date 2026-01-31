using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using StackExchange.Redis;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services;
using Tech_Store.Services.Admin.NotificationServices;
using Tech_Store.Services.Client.RecommendServices;
using X.PagedList.Extensions;

namespace Tech_Store.Controllers
{
    public class HomeController : BaseController
    {
		private readonly ILogger<HomeController> _logger;
		private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;
        private readonly RedisService _redis;
        private readonly RecommendServices _recommendServices;
        // Khai báo chỉ cần ILogger và IConfiguration
        public HomeController(ILogger<HomeController> logger, NotificationService notificationService, IConfiguration configuration, ApplicationDbContext context,
            RedisService redis, RecommendServices recommendServices)
			: base(context) // Gọi constructor của BaseController
		{
			_logger = logger;
			_configuration = configuration;
            _notificationService = notificationService;
            _redis = redis;
            _recommendServices = recommendServices;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy danh sách danh mục có trạng thái hiển thị
            var list_cate = _context.Categories
                .Include(p => p.Products)
                .Where(x => x.Visible == 1)
                .ToList();

            var list_hotSearch = await _redis.GetTopSearchKeywords(list_cate.ToList(),6);
            var hotSearchByCategory = new Dictionary<string, List<string>>();
            foreach (var category in list_cate)
            {
                var topKeywords = await _redis.GetTopSearchKeywordsByCategory(category.EngTitle, 12);
                hotSearchByCategory[category.EngTitle] = topKeywords;
            }

            //Lấy các brands theo danh mục
            var brandsByCategory = list_cate.ToDictionary(
                category => category.CategoryId,
                category => _context.Brands
                    .Where(b => b.CategoryId == category.CategoryId || b.CategoryId == null)
                    .ToList()
            );
            ViewBag.BrandsByCategory = brandsByCategory;
            // Lấy tối đa 10 sản phẩm cho mỗi danh mục
            var productsByCategory = list_cate.Select(category => new
            {
                Category = category,
                Products = _context.Products
                    .Include(p=> p.Brand)
                    .Where(p => p.CategoryId == category.CategoryId && p.Stock >= 1 && p.Status != "outstock")
                    .Take(10)
                    .OrderByDescending(p => p.ProductId)
                    .ToList()
            }).ToList();

            // Lấy danh sách sản phẩm Hot Sale
            var hotSaleProducts = _context.Products
                .Where(x => x.Visible == true && x.Stock >=1 && x.Status !="outstock")
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
            ViewBag.List_TopHotSearch = list_hotSearch;
            ViewBag.HotSearchByCategory = hotSearchByCategory;

            return View();
        }


        [OutputCache(Duration = 60, VaryByRouteValueNames = new[] { "slug" })]
        [Route("View/{slug}")]
        public async Task<IActionResult> View(string slug)
        {
            var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

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

            // Load specs in a single query
            var specs = await _context.Species
                .AsNoTracking()
                .Include(s => s.SpecValues.Where(v => v.ProductId == product.ProductId))
                .Where(s => s.SpecValues.Any(v => v.ProductId == product.ProductId))
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

            // Load recommended products
            var productSysId = product.ProductSysId;
            HttpContext.Items["product_sys_id"] = productSysId;

            var recommendResult = await _recommendServices.GetSceneRecommend(userId, "detail", productSysId, 10);
       
            // Load related products
            var relatedProducts = await _context.Products
                .AsNoTracking()
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId && p.Stock > 0 && p.Status != "outstock")
                .Take(10)
                .ToListAsync();

            // Pass data to the view
            ViewBag.specs = specs;
            ViewBag.review_count = reviewCount;
            ViewBag.reviews = reviews;
            ViewBag.ratingSummary = ratingSummary;
            ViewBag.averageRating = averageRating;
            ViewBag.comments = commentTree;
            ViewBag.related_products = relatedProducts;

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
            var products = _context.Products.Where(x => x.Category.EngTitle.Contains(eng_title) && x.Status != "outstock" && x.SellPrice > 0).
                OrderByDescending(x=>x.ProductId).ToPagedList(pageNumber, pageSize); // Áp dụng phân trang;

            var cate = _context.Categories.FirstOrDefault(x => x.EngTitle.Contains(eng_title));

            var list_brand = _context.Brands.Where(s=>s.CategoryId == cate.CategoryId || s.CategoryId == null).ToList();

            if(cate != null)
            {
                var list_HotSearch = _redis.GetTopSearchKeywordsByCategory(cate.EngTitle, 10).Result;
                ViewBag.list_HotSearch = list_HotSearch;
            }
           
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
            var list_brand = _context.Brands.Where(s => s.CategoryId == cate.CategoryId || s.CategoryId == null).ToList();
            ViewBag.list_brand = list_brand;
            ViewBag.Category = cate;
  
            // Trả về view
            return View("Category", products);
        }

        [Route("Brand/{brand_name}")]
        public IActionResult Brand(string brand_name, int? page)
        {
            int pageSize = 20;
            int pageNumber = page ?? 1;

            // Lấy sản phẩm theo brand
            var products = _context.Products
                .Where(x => x.Brand.Name.Contains(brand_name) && x.Status != "outstock" && x.SellPrice > 0)
                .OrderByDescending(x => x.ProductId )
                .ToPagedList(pageNumber, pageSize);

            // Lấy brand hiện tại
            var brand = _context.Brands
                .FirstOrDefault(x => x.Name.Contains(brand_name));

            // Lấy danh sách brand cùng category
            var list_brand = _context.Brands
                .Where(x => x.CategoryId == brand.CategoryId || x.CategoryId == null)
                .ToList();

            // Hot search theo category của brand
            if (brand?.Category != null)
            {
                var list_HotSearch = _redis
                    .GetTopSearchKeywordsByCategory(brand.Category.EngTitle, 10)
                    .Result;

                ViewBag.list_HotSearch = list_HotSearch;
            }

            ViewBag.list_brand = list_brand;
            ViewBag.Brand = brand;
            ViewBag.Category = brand?.Category;

            return View(products);
        }
        [HttpGet("Brand/FilterBrand")]
        public async Task<IActionResult> FilterBrand(string? key,string? order,string? price,string? brand,int pageNumber = 1)
        {
            int pageSize = 20;

            var currentBrand = _context.Brands.Include(b => b.Category).FirstOrDefault(b => b.Name == brand);

            if (currentBrand == null)
                return NotFound();

            // Keywords
            var keywords = key?.ToLower()
                .Split(" ", StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>();

            IQueryable<Models.Product> query = _context.Products
                   .Include(p => p.Category)
                   .Include(p => p.Brand)
                   .Where(p =>!string.IsNullOrEmpty(brand) &&
                       p.Brand != null &&
                       p.Brand.Name.ToLower() == brand.ToLower()
                   )
                   .Where(p =>string.IsNullOrEmpty(key) || keywords.All(word =>
                           (p.Name != null && p.Name.ToLower().Contains(word)) ||
                           (p.Description != null && p.Description.ToLower().Contains(word))
                       )
                   );


            // 🔥 Hot search (theo category của brand)
            if (!string.IsNullOrWhiteSpace(key) && key.Length >= 3)
            {
                var keyword = key.Trim().ToLower();
                if (Regex.IsMatch(keyword, @"^[a-zA-Z0-9\s]+$"))
                {
                    await _redis.IncrementHotKeywordAsync(
                        keyword,
                        currentBrand.Category?.EngTitle ?? "all"
                    );
                }
            }

            // Order
            query = order switch
            {
                "alphabet" => query.OrderBy(x => x.Name),
                "alphabet_desc" => query.OrderByDescending(x => x.Name),
                "price" => query.OrderBy(x => x.SellPrice),
                "price_desc" => query.OrderByDescending(x => x.SellPrice),
                "care" => query.OrderByDescending(x => x.Reviews),
                _ => query.OrderByDescending(x => x.ProductId)
            };

            // Price filter
            if (!string.IsNullOrEmpty(price))
            {
                query = price switch
                {
                    "max5" => query.Where(x => x.SellPrice < 5_000_000),
                    "max10" => query.Where(x => x.SellPrice >= 5_000_000 && x.SellPrice < 10_000_000),
                    "max20" => query.Where(x => x.SellPrice >= 10_000_000 && x.SellPrice < 20_000_000),
                    "max50" => query.Where(x => x.SellPrice >= 20_000_000 && x.SellPrice < 50_000_000),
                    "more" => query.Where(x => x.SellPrice >= 50_000_000),
                    _ => query
                };
            }

            // Paging
            var products = query.ToPagedList(pageNumber, pageSize);

            // ViewBag (⚠️ đồng bộ với Brand.cshtml)
            ViewBag.Brand = currentBrand;
            ViewBag.Category = currentBrand.Category;
            ViewBag.Order = order;
            ViewBag.Price = price;
            ViewBag.Key = key;

            ViewBag.list_brand = _context.Brands
                .Where(b => b.CategoryId == currentBrand.CategoryId || b.CategoryId == null)
                .ToList();

            return View("Brand", products);
        }


        [Route("Search/{key?}")]
        public async Task<IActionResult> SearchAsync(string key, int? page)
        {
            int pageSize = 20;
            int pageNumber = page ?? 1;

            var products = _context.Products.Include(p => p.Category)
                 .Where(x => string.IsNullOrEmpty(key) || x.Name.Contains(key))
                 .Select(x => new
                 {
                     Product = x,
                     Score =
                         x.Name == key ? 3 :
                         x.Name.StartsWith(key) ? 2 :
                         x.Name.Contains(key) ? 1 : 0
                 })
                 .OrderByDescending(x => x.Score)
                 .ThenByDescending(x => x.Product.ProductId)
                 .Select(x => x.Product)
                 .ToPagedList(pageNumber, pageSize);


            var cate = _context.Categories.ToList();
            var list_brand = _context.Brands.ToList();
            ViewBag.list_brand = list_brand;
            ViewBag.list_category = cate;
            ViewBag.Key = key;
            return View(products);
        }

        [HttpGet("Search/Filter")]
        public async Task<IActionResult> FilterAsync(string key,string? category , string? order, string? price, string? brand, int pageNumber = 1)
        {
            int pageSize = 20;

            // Truy vấn cơ bản
            var keywords = key?.ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();

            IQueryable<Models.Product> query = _context.Products
                .Include(x => x.Category)
                .Where(p =>
                    string.IsNullOrEmpty(key) || keywords.All(word =>
                        (p.Name != null && p.Name.ToLower().Contains(word)) ||
                        (p.Description != null && p.Description.ToLower().Contains(word))
                    )
                );

            // Gọi Redis lưu hot search keyword nếu hợp lệ
            if (!string.IsNullOrWhiteSpace(key) && key.Length >= 3)
            {
                var keyword = key.Trim().ToLower();

                if (Regex.IsMatch(keyword, @"^[a-zA-Z0-9\s]+$"))
                {
                    // Lấy category có nhiều sản phẩm khớp keyword nhất
                    var matchedCategorySlug = _context.Categories
                        .Select(c => new
                        {
                            c.EngTitle,
                            MatchCount = c.Products.Count(p =>
                                keywords.All(word =>
                                    p.Name.ToLower().Contains(word) ||
                                    (p.Description != null && p.Description.ToLower().Contains(word))
                                )
                            )
                        })
                        .OrderByDescending(x => x.MatchCount)
                        .FirstOrDefault(x => x.MatchCount > 0)?.EngTitle ?? "all";

                    await _redis.IncrementHotKeywordAsync(keyword, matchedCategorySlug);
                }
            }
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
            if (!string.IsNullOrEmpty(category) && int.TryParse(category, out int categoryIdValue))
            {
                query = query.Where(x => x.CategoryId == categoryIdValue);
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
            ViewBag.Category = category;
            ViewBag.Key = key;

            var cate = _context.Categories.ToList();
            var list_brand = _context.Brands.ToList();
            ViewBag.list_brand = list_brand;
            ViewBag.list_category = cate;

            // Trả về view
            return View("Search", products);
        }

    }
}
