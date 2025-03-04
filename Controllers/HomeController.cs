using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.Diagnostics;
using System.Drawing.Printing;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.NotificationServices;
using X.PagedList.Extensions;

namespace Tech_Store.Controllers
{
    public class HomeController : BaseController
    {
		private readonly ILogger<HomeController> _logger;
		private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;

        // Khai báo chỉ cần ILogger và IConfiguration
        public HomeController(ILogger<HomeController> logger, NotificationService notificationService, IConfiguration configuration, ApplicationDbContext context)
			: base(context) // Gọi constructor của BaseController
		{
			_logger = logger;
			_configuration = configuration;
            _notificationService = notificationService;
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
                 .ThenInclude(vp => vp.VariantAttributes) 
                     .ThenInclude(a => a.AttributeValue)  
             .Include(x => x.Galleries) 
             .Include(x => x.Reviews)  
             .Include(x => x.Category)  
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


            var comments = _context.Comments
              .Include(r => r.User) // Bao gồm thông tin người dùng
              .Where(r => r.ProductId == product.ProductId && r.Status == true) // Lọc sản phẩm và trạng thái
              .AsNoTracking()
              .ToList();

            // Nhóm các bình luận cha và con
            var commentTree = comments
                .Where(c => c.ParentCommentId == null) // Lấy các bình luận cha
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
                    UserName = $"{c.User.LastName} {c.User.FirstName}", // Tên người dùng
                    UserAvatar = c.User.Img, // Ảnh đại diện

                    // Tìm và chuyển đổi các bình luận con
                    Replies = comments
                        .Where(r => r.ParentCommentId == c.CommentId) // Tìm các bình luận con
                        .OrderBy(r => r.CreatedAt) // Sắp xếp bình luận con theo thời gian
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
                            UserName = $"{r.User.LastName} {r.User.FirstName}", // Tên người dùng
                            UserAvatar = r.User.Img // Ảnh đại diện
                        })
                        .ToList()
                })
                .OrderByDescending(c => c.CreatedAt) // Sắp xếp cha theo thời gian giảm dần
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
            ViewBag.comments = commentTree;
            ViewBag.related_products = _context.Products
                .Where(p => p.CategoryId == product.CategoryId && p.ProductId != product.ProductId)
                .Take(10)
                .AsNoTracking()
                .ToList();

            return View(product);
        }

        //[HttpGet("GetVariants/{productId}")]
        //public IActionResult GetVariants(int productId)
        //{
        //    var variants = (from vp in _context.VarientProducts
        //                    join va in _context.VariantAttributes on vp.VarientId equals va.ProductVariantId
        //                    join av in _context.AttributeValues on va.AttributeValueId equals av.AttributeValueId
        //                    join a in _context.Attributes on av.AttributeId equals a.AttributeId
        //                    where vp.ProductId == productId
        //                    select new
        //                    {
        //                        AttributeName = a.Name,
        //                        Value = av.Value,
        //                        VariantId = vp.VarientId
        //                    }).ToList();

        //    var groupedVariants = variants.GroupBy(v => v.AttributeName)
        //                                  .ToDictionary(g => g.Key, g => g.Select(v => new { v.Value, v.VariantId }).Distinct());

        //    return Ok(groupedVariants);
        //}

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
            var list_brand = _context.Brands.Where(s=>s.CategoryId == cate.CategoryId || s.CategoryId == null).ToList();
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

        [Route("Search/{key?}")]
        public IActionResult Search(string key, int? page)
        {
            int pageSize = 20;
            int pageNumber = page ?? 1;

            var products = _context.Products
              .Where(x => string.IsNullOrEmpty(key) || x.Name.Contains(key))  // Kiểm tra nếu key trống thì không lọc
              .OrderByDescending(x => x.ProductId)
              .ToPagedList(pageNumber, pageSize); // Áp dụng phân trang

            var cate = _context.Categories.ToList();
            var list_brand = _context.Brands.ToList();
            ViewBag.list_brand = list_brand;
            ViewBag.list_category = cate;
            ViewBag.Key = key;
            return View(products);
        }

        [HttpGet("Search/Filter")]
        public IActionResult Filter(string key,string? category , string? order, string? price, string? brand, int pageNumber = 1)
        {
            int pageSize = 20;

            // Truy vấn cơ bản
            IQueryable<Models.Product> query = _context.Products
                .Where(x => string.IsNullOrEmpty(key) || x.Name.Contains(key));  // Kiểm tra nếu key trống thì không lọc;

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
