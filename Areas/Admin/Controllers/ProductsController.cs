using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using QRCoder;
using System.Diagnostics;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.NotificationServices;
using static System.Net.Mime.MediaTypeNames;
using Product = Tech_Store.Models.Product;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]

    public class ProductsController : BaseAdminController
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly NotificationService _notificationService;
        public ProductsController(ApplicationDbContext context, IConfiguration configuration, IWebHostEnvironment webHostEnvironment, NotificationService notificationService) : base(context)
        {
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
        }


        private List<Category> GetListCategories()
        {
            // Chọn nhiều trường từ Categories
            var categories = _context.Categories
                .Select(x => new Category
                {
                    CategoryId = x.CategoryId,
                    Name = x.Name
                })
                .ToList();

            return categories;
        }

        private List<Brand> GetListBrand()
        {
            // Chọn nhiều trường từ Brands
            var brands = _context.Brands.Select(x => new Brand
            {
                BrandId = x.BrandId,
                Name = x.Name
            })
                .ToList();

            return brands;
        }
        private List<Models.Attribute> GetListAttribute()
        {
            var attributes = _context.Attributes.Include(p => p.AttributeValues)
                .Select(x => new Models.Attribute
            {
                AttributeId = x.AttributeId,
                Name = x.Name,
                Code = x.Code,
                AttributeValues = x.AttributeValues,
                SortOrder = x.SortOrder
            }).OrderBy(x=>x.SortOrder).ToList();

            return attributes;
        }
        [Route("View/{id}")]
        public async Task<IActionResult> View(int id)
        {

            var detail = await _context.Products.Include(x => x.Category).Include(x => x.Brand)
                .Include(x => x.Galleries).Include(x => x.Reviews).Include(x => x.VarientProducts).
                FirstOrDefaultAsync(x => x.ProductId == id);
            var review = await _context.Reviews.Where(x=>x.ProductId == id).ToListAsync();
            var order = await _context.OrderItems.Where(x => x.ProductId == id).ToListAsync();
            var total_sell = order.Sum(x => x.Price);
            ViewBag.Total_Sell = total_sell;
            ViewBag.Review_Count = review.Count();
            ViewBag.Order_Count = order.Count();

            return View(detail);
        }

        [Route("{status?}")]
        [Route("Index/{status?}")] // Thêm dấu hỏi để status có thể là null
        public async Task<IActionResult> Index(string? status)
        {
            // Khai báo danh sách sản phẩm
            IQueryable<Models.Product> query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.ProductId);

            // Nếu status không phải là null và không phải là "all", lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(p => p.Status == status);
            }
            var list_cate = _context.Categories.ToList();
            ViewBag.cate = list_cate;
            var list_brand = _context.Brands.ToList();
            ViewBag.brand = list_brand;
            // Lấy danh sách sản phẩm từ cơ sở dữ liệu
            var list_products = await query.ToListAsync();

            return View(list_products);
        }
        [HttpPost]
        [Route("Filter")]
        public IActionResult Filter(string sku, string name, string status, int? categoryId, int? brandId, int? stockFrom, int? stockTo)
        {
            var products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsQueryable();

            // Áp dụng các tiêu chí lọc
            if (!string.IsNullOrEmpty(sku))
                products = products.Where(p => p.Sku.Contains(sku.Trim()));
            if (!string.IsNullOrEmpty(name))
                products = products.Where(p => p.Name.Contains(name.Trim()));
            if (!string.IsNullOrEmpty(status))
                products = products.Where(p => p.Status == status);
            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);
            if (brandId.HasValue)
                products = products.Where(p => p.BrandId == brandId.Value);
            if (stockFrom.HasValue)
                products = products.Where(p => p.Stock >= stockFrom.Value);
            if (stockTo.HasValue)
                products = products.Where(p => p.Stock <= stockTo.Value);

            // Chuyển đổi sang view model để trả về JSON
            var result = products.OrderByDescending(p => p.ProductId)
                .Take(100)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Image = p.Image,
                    Name = p.Name,
                    Sku = p.Sku,
                    BrandName = p.Brand.Name,
                    CategoryName = p.Category.Name,
                    SellPrice = p.SellPrice,
                    Stock = p.Stock,
                    Visible = (bool)p.Visible,
                    Status = p.Status
                })
                .ToList();

            return Json(result);
        }
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = GetListCategories();
            ViewBag.Brands = GetListBrand();
            ViewBag.Attributes = GetListAttribute();
            return View();
        }
        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDTo productDto)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var product = new Models.Product
                    {
                        Name = productDto.Name,
                        Slug = GenerateSlug(productDto.Name),
                        Sku = productDto.Sku,
                        Description = productDto.Description,
                        CostPrice = productDto.CostPrice,
                        OriginalPrice = productDto.OriginalPrice,
                        SellPrice = productDto.SellPrice,
                        DiscountAmount = productDto.DiscountAmount,
                        DiscountPercentage = productDto.DiscountPercentage,
                        Stock = productDto.Stock,
                        CategoryId = productDto.CategoryId,
                        BrandId = productDto.BrandId,
                        Status = productDto.Status,
                        WarrantyPeriod = productDto.WarrantyPeriod,
                        UrlYoutube = productDto.UrlYoutube,
                        Weight = productDto.Weight,
                        Visible = true
                        // Thêm các thuộc tính khác của Product nếu cần
                    };

                    // Lưu hình ảnh chính của sản phẩm từ Request
                    var imageFile = Request.Form.Files.FirstOrDefault();
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var fileName = $"SP_{product.Sku}.webp";
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                        using (var stream = new FileStream(imagePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream);
                        }

                        product.Image = fileName;
                    }
                    else
                    {
                        product.Image = "none.jpg";
                    }

                    // Thêm sản phẩm vào cơ sở dữ liệu
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Thêm các biến thể sản phẩm
                    if (productDto.VarientProducts != null && productDto.VarientProducts.Any())
                    {
                        foreach (var variantDto in productDto.VarientProducts)
                        {
                            var variant = new VarientProduct
                            {
                                ProductId = product.ProductId,
                                Sku = variantDto.Sku,
                                Attributes = variantDto.Attributes,
                                Stock = variantDto.Stock ?? 0,
                                Price = variantDto.Price ?? product.SellPrice
                            };
                            _context.VarientProducts.Add(variant);
                            await _context.SaveChangesAsync();

                            // Liên kết biến thể và thuộc tính
                            if ((bool)productDto.IsUseVariant)
                            {
                                // Chuỗi sẽ có dạng rom:16GB,ram:64GB
                                var attributePairs = variantDto.Attributes.Split(',');

                                foreach (var attributePair in attributePairs)
                                {
                                    // Cắt lấy "rom" và "16GB"
                                    var attrParts = attributePair.Split(':');
                                    if (attrParts.Length != 2) continue; // Bỏ qua nếu không đúng định dạng

                                    var attr = attrParts[0].Trim(); // Lấy thuộc tính (e.g., "rom")
                                    var attrValue = attrParts[1].Trim(); // Lấy giá trị (e.g., "16GB")

                                    // Truy vấn bảng Attributes
                                    var attrQuery = await _context.Attributes.FirstOrDefaultAsync(x => x.Code == attr);
                                    if (attrQuery == null) continue; // Bỏ qua nếu không tìm thấy

                                    // Truy vấn bảng AttributeValues
                                    var attrValueQuery = await _context.AttributeValues.FirstOrDefaultAsync(x =>
                                        x.Value == attrValue && x.AttributeId == attrQuery.AttributeId);
                                    if (attrValueQuery == null) continue; // Bỏ qua nếu không tìm thấy

                                    // Tạo dòng mới trong bảng VariantAttribute
                                    var productAttribute = new VariantAttribute
                                    {
                                        ProductVariantId = variant.VarientId,
                                        AttributeValueId = attrValueQuery.AttributeValueId,
                                    };

                                    _context.VariantAttributes.Add(productAttribute);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    //Nếu người dùng không muốn sử dụng đa biến thể SP
                    else
                    {
                        var defaultVariant = new VarientProduct
                        {
                            ProductId = product.ProductId,
                            Sku = product.Sku,
                            Attributes = "",
                            Stock = product.Stock,
                            Price = product.SellPrice
                        };

                        _context.VarientProducts.Add(defaultVariant);
                    }

                    // Thêm danh sách hình ảnh vào bảng Gallery
                    // Kiểm tra nếu danh sách product.Galleries chứa các tệp IFormFile
                    if (productDto.Galleries != null && productDto.Galleries.Any())
                    {
                        foreach (var galleryFile in productDto.Galleries)
                        {
                            if (galleryFile != null && galleryFile is IFormFile file && file.Length > 0) // Chỉ xử lý nếu là tệp tin hợp lệ
                            {
                                var galleryFileName = $"GSP_{product.Sku}_{Guid.NewGuid()}.webp";
                                var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", galleryFileName);
                                Directory.CreateDirectory(Path.GetDirectoryName(galleryImagePath));

                                using (var stream = new FileStream(galleryImagePath, FileMode.Create))
                                {
                                    await file.CopyToAsync(stream); // Sử dụng CopyToAsync cho IFormFile
                                }

                                // Thêm ảnh vào cơ sở dữ liệu
                                var galleryItem = new Gallery
                                {
                                    ProductId = product.ProductId,
                                    Path = galleryFileName
                                };
                                _context.Galleries.Add(galleryItem);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }


                    await transaction.CommitAsync();
                    await GenerateProductsJson();

                    //Thông báo 
                    await _notificationService.NotifyAsync(Events.NotificationTarget.Admins, "Sản phẩm mới", $"Sản phẩm {product.Name} được thêm vào danh sách", "product added", $"/admin/products/views/{product.ProductId}");

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình lưu trữ: " + ex.Message);
                    return View("Error");
                }
            }
            return BadRequest("Invalid");
        }
        #region Update Project
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var _product = await _context.Products.Include(p => p.VarientProducts)
                .Include(p => p.Brand).Include(p => p.Category).Include(p => p.Galleries)
                .FirstOrDefaultAsync(x => x.ProductId == id);
            if (_product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            //lấy variant_id
            var variant_id = await _context.VarientProducts
           .Where(x => x.ProductId == _product.ProductId)
           .Select(x => x.VarientId) // Sửa chỗ này: chọn VarientId thay vì ProductId
           .ToListAsync(); // Sửa chỗ này: thêm Async()

            // Lấy danh sách Attribute ID
            var productAttributes = await _context.VariantAttributes
                .Where(pa => variant_id.Contains(pa.ProductVariantId)) // Sửa chỗ này: dùng .Contains()
                .Include(pa => pa.AttributeValue)
                .Select(pa => pa.AttributeValue.AttributeId)
                .Distinct()
                .ToListAsync();


            ViewBag.Attributes_checked = productAttributes;
            ViewBag.Categories = GetListCategories();
            ViewBag.Brands = GetListBrand();
            ViewBag.Attributes = GetListAttribute();
            return View(_product);
        }

        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProductDTo productDto)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid model state");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(productDto.ProductId);
                if (product == null) return NotFound();

                if (productDto.Image != null) await UpdateMainImage(product, productDto.Image);

                UpdateProductCoreFields(product, productDto);
                await ProcessVariants(productDto);
                if (productDto.Galleries != null) await AddGalleryImages(product, productDto.Galleries);

                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await GenerateProductsJson();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log exception here
                return StatusCode(500, "Có lỗi xảy ra khi cập nhật");
            }
        }

        private async Task UpdateMainImage(Product product, IFormFile newImage)
        {
            var fileName = $"SP_{product.Sku}_{Guid.NewGuid()}.webp";
            var imagePath = Path.Combine("wwwroot/Upload/Products", fileName);

            // Delete old image if exists
            if (!string.IsNullOrEmpty(product.Image))
            {
                var oldPath = Path.Combine("wwwroot/Upload/Products", product.Image);
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            using var stream = new FileStream(imagePath, FileMode.Create);
            await newImage.CopyToAsync(stream);
            product.Image = fileName;
        }

        private void UpdateProductCoreFields(Product product, ProductDTo dto)
        {
            product.Name = dto.Name;
            product.Slug = GenerateSlug(dto.Name);
            product.Description = dto.Description;
            product.OriginalPrice = dto.OriginalPrice;
            product.CostPrice = dto.CostPrice;
            product.SellPrice = dto.SellPrice;
            product.WarrantyPeriod = dto.WarrantyPeriod;
            product.Status = dto.Status;
            product.BrandId = dto.BrandId;
            product.Stock = dto.Stock;
            product.Weight = dto.Weight;
            product.CategoryId = dto.CategoryId;
            product.UrlYoutube = dto.UrlYoutube;
            product.Sku = dto.Sku;
            product.Color = dto.Color;

            // Handle discounts
            product.DiscountAmount = dto.DiscountAmount;
            product.DiscountPercentage = dto.DiscountAmount.HasValue ? null : dto.DiscountPercentage;
        }

        private async Task ProcessVariants(ProductDTo dto)
        {
            if (dto.VarientProducts == null) return;

            var existingVariants = await _context.VarientProducts
                .Where(v => v.ProductId == dto.ProductId)
                .ToListAsync();

            var variantSkus = dto.VarientProducts.Select(v => v.Sku).ToHashSet(); // Dùng HashSet để tối ưu tìm kiếm
            var removedVariants = existingVariants.Where(ev => !variantSkus.Contains(ev.Sku)).ToList();

            // Kiểm tra và xóa các VariantAttribute trước khi xóa VariantProduct
            var removedVariantIds = removedVariants.Select(v => v.VarientId).ToList();
            var variantAttributesToRemove = await _context.VariantAttributes
                .Where(va => removedVariantIds.Contains(va.ProductVariantId))
                .ToListAsync();

            // Xóa các VariantAttributes tham chiếu đến các variant bị xóa
            _context.VariantAttributes.RemoveRange(variantAttributesToRemove);

            // Kiểm tra xem có OrderItem nào tham chiếu đến VariantProduct không
            var referencedVariantIds = await _context.OrderItems
                .Where(oi => removedVariantIds.Contains(oi.VarientProductId))
                .Select(oi => oi.VarientProductId)
                .ToListAsync();

            // Chỉ xóa những VariantProduct không có tham chiếu
            var safeToDelete = removedVariants.Where(v => !referencedVariantIds.Contains(v.VarientId)).ToList();
            _context.VarientProducts.RemoveRange(safeToDelete);

            // Update/Add variants
            foreach (var variantDto in dto.VarientProducts)
            {
                var existing = existingVariants.FirstOrDefault(v => v.Sku == variantDto.Sku);
                if (existing != null)
                {
                    existing.Attributes = variantDto.Attributes;
                    existing.Stock = variantDto.Stock ?? 0;
                    existing.Price = variantDto.Price ?? dto.SellPrice ?? 0;
                }
                else
                {
                    _context.VarientProducts.Add(new VarientProduct
                    {
                        ProductId = dto.ProductId,
                        Sku = variantDto.Sku,
                        Attributes = variantDto.Attributes,
                        Stock = variantDto.Stock ?? 0,
                        Price = variantDto.Price ?? dto.SellPrice ?? 0
                    });
                }
            }

            await _context.SaveChangesAsync();
            await ProcessVariantAttributes(dto);
        }


        private async Task ProcessVariantAttributes(ProductDTo dto)
        {
            if ((bool)!dto.IsUseVariant) return;

            var variants = await _context.VarientProducts
                .Where(v => v.ProductId == dto.ProductId)
                .ToListAsync();

            // Xóa các VariantAttributes cũ trước khi thêm mới
            var existingAttributes = await _context.VariantAttributes
                .Where(va => va.ProductVariantId == dto.ProductId)
                .ToListAsync();
            _context.VariantAttributes.RemoveRange(existingAttributes);

            foreach (var variant in variants)
            {
                var attributes = variant.Attributes?.Split(',') ?? Array.Empty<string>();

                foreach (var attrPair in attributes)
                {
                    var parts = attrPair.Split(':');
                    if (parts.Length != 2) continue;

                    var attribute = await _context.Attributes
                        .FirstOrDefaultAsync(a => a.Code == parts[0].Trim());

                    var value = await _context.AttributeValues
                        .FirstOrDefaultAsync(av => av.AttributeId == attribute.AttributeId
                            && av.Value == parts[1].Trim());

                    if (attribute == null || value == null) continue;

                    // Kiểm tra xem VariantAttribute đã tồn tại chưa để tránh trùng lặp
                    var existingVariantAttribute = await _context.VariantAttributes
                        .FirstOrDefaultAsync(va => va.ProductVariantId == variant.VarientId
                                                   && va.AttributeValueId == value.AttributeValueId);

                    // Nếu không tồn tại thì thêm mới
                    if (existingVariantAttribute == null)
                    {
                        _context.VariantAttributes.Add(new VariantAttribute
                        {
                            ProductVariantId = variant.VarientId,
                            AttributeValueId = value.AttributeValueId
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();
        }


        private async Task AddGalleryImages(Product product, IEnumerable<IFormFile> images)
        {
            foreach (var image in images)
            {
                if (image.Length == 0) continue;

                var fileName = $"GSP_{product.Sku}_{Guid.NewGuid()}.webp";
                var path = Path.Combine("wwwroot/Upload/Products", fileName);

                using var stream = new FileStream(path, FileMode.Create);
                await image.CopyToAsync(stream);

                _context.Galleries.Add(new Gallery
                {
                    ProductId = product.ProductId,
                    Path = fileName
                });
            }
        }
        #endregion
        //Cho phép hiển thị sản phẩm phía CLIENT hay ko
        [HttpPost("ChangeVisible")]
        public async Task<JsonResult> ChangeVisible(int productId)
        {
            if (productId == 0)
            {
                return Json(new { success = false, message = "Không nhận được mã sản phẩm" });
            }
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId.Equals(productId));
            if (product == null)
            {

                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }
            // Đảo ngược giá trị Visible
            product.Visible = product.Visible == false ? true : false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thay đổi thành công", visible = product.Visible });
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            // Tìm sản phẩm
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return BadRequest("Không tìm thấy sản phẩm");
            }

            // Lấy tất cả các bản ghi liên quan
            var variantProducts = await _context.VarientProducts.Where(x => x.ProductId == id).ToListAsync();
            var galleries = await _context.Galleries.Where(x => x.ProductId == id).ToListAsync();
            var cartItems = await _context.CartItems.Where(x => x.ProductId == id).ToListAsync();
            var reviews = await _context.Reviews.Where(x => x.ProductId == id).ToListAsync();
            var wishlists = await _context.Wishlists.Where(x => x.ProductId == id).ToListAsync();
            var orderItems = await _context.OrderItems.Where(x => x.ProductId == id).ToListAsync();
            var variantProductAttrs = await _context.VariantAttributes
                                        .Where(x => x.ProductVariantId == id).ToListAsync();

            // Nếu sản phẩm đã được mua, chỉ ẩn và khóa lại
            if (orderItems.Any())
            {
                product.Status = "discontinued"; // Khóa sản phẩm
                product.Visible = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Sản phẩm đã được giao dịch, chỉ có thể Khóa/Ẩn" });
            }

            // Dùng TransactionScope để đảm bảo tính toàn vẹn
            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // Xóa tất cả các thuộc tính của biến thể trước
                    if (variantProductAttrs.Any())
                    {
                        _context.VariantAttributes.RemoveRange(variantProductAttrs);
                        await _context.SaveChangesAsync();
                    }

                    // Xóa dữ liệu liên quan khác
                    if (galleries.Any()) _context.Galleries.RemoveRange(galleries);
                    if (cartItems.Any()) _context.CartItems.RemoveRange(cartItems);
                    if (reviews.Any()) _context.Reviews.RemoveRange(reviews);
                    if (wishlists.Any()) _context.Wishlists.RemoveRange(wishlists);

                    await _context.SaveChangesAsync();

                    // Xóa biến thể sản phẩm
                    if (variantProducts.Any())
                    {
                        _context.VarientProducts.RemoveRange(variantProducts);
                        await _context.SaveChangesAsync();
                    }

                    // Xóa sản phẩm cuối cùng
                    _context.Products.Remove(product);
                    await _context.SaveChangesAsync();

                    // Xóa JSON cache
                    await GenerateProductsJson();

                    // Commit transaction sau khi mọi thứ thành công
                    await transaction.CommitAsync();

                    // Xóa hình ảnh sau khi xóa thành công trong DB
                    if (product.Image != null)
                    {
                        var productImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", product.Image);
                        if (System.IO.File.Exists(productImagePath))
                        {
                            System.IO.File.Delete(productImagePath);
                        }
                    }

                    // Xóa hình ảnh của Galleries sau khi xóa DB
                    foreach (var gallery in galleries)
                    {
                        var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", gallery.Path);
                        if (System.IO.File.Exists(galleryImagePath))
                        {
                            System.IO.File.Delete(galleryImagePath);
                        }
                    }

                    return Json(new { success = true, message = "Sản phẩm đã được xóa thành công khỏi danh sách." });
                }
                catch (Exception ex)
                {
                    // Rollback transaction nếu có lỗi
                    await transaction.RollbackAsync();
                    return BadRequest(new { success = false, message = "Lỗi khi xóa sản phẩm: " + ex.Message });
                }
            }
        }


        // Hàm dùng để xóa ảnh thumbnail của sản phẩm
        // Đang sử dụng Kết hợp với FilePond
        [HttpPost("DeleteFileImage")]
        public async Task<IActionResult> DeleteFileImage([FromBody] DeleteFileViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FilePath))
                    return Json(new { success = false, message = "Đường dẫn file không hợp lệ" });

                // Xử lý xóa ảnh thumbnail
                var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == model.ProductId);
                if (product.Image !=null)
                {
                    // Xóa file vật lý
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "Upload/Products", product.Image);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }

                    // Xóa ảnh trong DB
                    product.Image = null;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Ảnh không tồn tại" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra" });
            }
        }

        // Hàm dùng để xóa ảnh thư viện của sản phẩm
        // Đang sử dụng Kết hợp với FilePond
        [HttpPost("DeleteFileImageFromGalleries")]
        public async Task<IActionResult> DeleteFileImageFromGalleries([FromBody] DeleteFileViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FilePath))
                    return Json(new { success = false, message = "Đường dẫn file không hợp lệ" });

                var img = await _context.Galleries.FirstOrDefaultAsync(x => x.ProductId == model.ProductId && x.Path.Contains(model.FilePath));
                if (img == null)
                {
                    return NotFound("Không tìm thấy ảnh thư viện");
                }

                if (img !=null)
                {
                    // Xóa file vật lý
                    string fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "Upload/Products", model.FilePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }

                    // Xóa ảnh trong DB
                    _context.Galleries.Remove(img);
                    await _context.SaveChangesAsync();
                    return Json(new { success = true });
                }

                return Json(new { success = false, message = "Ảnh không tồn tại" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra" });
            }
        }

        //Tạo Code
        [HttpGet("GenerateCode/{id}/{content?}/{codeType?}")]
        public async Task<IActionResult> GenerateCode(int id, string? content, string? codeType)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            // Tạo URL với base URL từ môi trường hiện tại
            var path = Url.Action("View", "Home", new { id = id }, Request.Scheme);

            // Xử lý nội dung của mã
            var url = content != null && content == "url" ? path : product.Sku;

            // Tạo mã dựa trên loại code
            if (string.IsNullOrEmpty(codeType) || codeType == "QRCode")
            {
                ViewBag.QRCodeImage = GenerateQRCode(url);
            }
            else if (codeType == "Barcode")
            {
                ViewBag.BarcodeImage = GenerateBarcode(product.Sku);
            }

            ViewBag.Content = content;
            ViewBag.CodeType = codeType;

            return View(product);
        }

        //In Code
        [HttpGet]
        [Route("PrintCode")]
        public async Task<JsonResult> PrintCode(int id, string content, string codeType, int quantity)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found" });
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Template", "print-code-template.html");
            string templateHtml = System.IO.File.ReadAllText(path);

            var replace = new StringBuilder(templateHtml);
            var codeImages = new StringBuilder();

            // Tạo nhiều mã và chèn chúng vào
            for (int i = 1; i <= quantity; i++)
            {
                string codeImage = codeType == "Barcode" ? GenerateBarcode(product.Sku) : GenerateQRCode(content);

                // Tạo một phần tử hình ảnh cho mỗi mã
                codeImages.AppendLine($"<div>");
                codeImages.AppendLine($"<img src='{codeImage}' alt='Generated Code' />");
                codeImages.AppendLine($"<p> {product.Name}</p>");
                codeImages.AppendLine($"<p>{product.Sku}</p>");
                codeImages.AppendLine($"</div>");
            }

            // Thay thế placeholder @@CodeImage bằng danh sách các mã đã tạo
            replace.Replace("@@CodeImage", codeImages.ToString());
            replace.Replace("@@ProductName", product.Name);
            replace.Replace("@@ProductSku", product.Sku);
            await Task.Yield();
            return Json(new { success = true, html = replace.ToString() });
        }


        // Function to generate QR code
        private string GenerateQRCode(string content)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);  // Adjust size here
            return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
        }

        // Function to generate Barcode
        private string GenerateBarcode(string sku)
        {
            var barcodeWriter = new ZXing.BarcodeWriterPixelData
            {
                Format = ZXing.BarcodeFormat.CODE_128,
                Options = new ZXing.Common.EncodingOptions
                {
                    Height = 150,
                    Width = 300
                }
            };

            var pixelData = barcodeWriter.Write(sku);
            using (var barcodeBitmap = new System.Drawing.Bitmap(pixelData.Width, pixelData.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb))
            using (var barcodeStream = new MemoryStream())
            {
                var bitmapData = barcodeBitmap.LockBits(new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height),
                                                        System.Drawing.Imaging.ImageLockMode.WriteOnly,
                                                        System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                try
                {
                    System.Runtime.InteropServices.Marshal.Copy(pixelData.Pixels, 0, bitmapData.Scan0, pixelData.Pixels.Length);
                }
                finally
                {
                    barcodeBitmap.UnlockBits(bitmapData);
                }

                barcodeBitmap.Save(barcodeStream, System.Drawing.Imaging.ImageFormat.Png);
                return "data:image/png;base64," + Convert.ToBase64String(barcodeStream.ToArray());
            }
        }
        public static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // Chuyển đổi chữ thường và bỏ dấu
            string slug = RemoveDiacritics(name.ToLower());

            // Loại bỏ ký tự đặc biệt, chỉ giữ lại chữ cái, số và khoảng trắng
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Thay khoảng trắng và dấu gạch ngang liên tiếp thành một dấu gạch ngang
            slug = Regex.Replace(slug, @"\s+", "-").Trim();

            // Loại bỏ dấu gạch ngang dư thừa ở đầu và cuối
            slug = slug.Trim('-');

            return slug;
        }

        private static string RemoveDiacritics(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        private async Task GenerateProductsJson()
        {
            var products_list = await _context.Products.Select(x => new
            {
                x.ProductId,
                x.Name,
                x.Slug,
                x.Image,
                x.SellPrice,
                x.OriginalPrice
            }).ToListAsync();
            // Đường dẫn đến thư mục
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "json");

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            // Lưu dữ liệu ra file JSON
            var filePath = Path.Combine(path, "products.json");

            await System.IO.File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(products_list));
        }

    }
}
