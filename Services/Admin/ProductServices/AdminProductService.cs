using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using QRCoder;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;
using Tech_Store.Services.Admin.NotificationServices;
using Product = Tech_Store.Models.Product;
using X.PagedList;

namespace Tech_Store.Services.Admin.ProductServices
{
    public class AdminProductService : IAdminProductService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly NotificationService _notificationService;

        public AdminProductService(
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment,
            NotificationService notificationService)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _notificationService = notificationService;
        }

        public async Task<AdminProductDetailData> GetDetailAsync(int id)
        {
            var detail = await _context.Products
                .Include(x => x.Category)
                .Include(x => x.Brand)
                .Include(x => x.Galleries)
                .Include(x => x.Reviews).ThenInclude(x => x.User)
                .Include(x => x.VarientProducts)
                .FirstOrDefaultAsync(x => x.ProductId == id);

            var review = await _context.Reviews.Where(x => x.ProductId == id).ToListAsync();
            var order = await _context.OrderItems.Where(x => x.ProductId == id).ToListAsync();

            return new AdminProductDetailData
            {
                Product = detail,
                TotalSell = order.Sum(x => x.Price),
                ReviewCount = review.Count,
                OrderCount = order.Count
            };
        }

        public async Task<AdminProductIndexData> GetIndexDataAsync(AdminProductFilterRequest request)
        {
            IQueryable<Product> query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsNoTracking();

            if (!string.IsNullOrEmpty(request.Status) && request.Status != "all")
            {
                query = query.Where(p => p.Status == request.Status);
            }

            if (!string.IsNullOrEmpty(request.Sku))
            {
                query = query.Where(p => p.Sku != null && p.Sku.Contains(request.Sku.Trim()));
            }

            if (!string.IsNullOrEmpty(request.Name))
            {
                query = query.Where(p => p.Name.Contains(request.Name.Trim()));
            }

            if (request.CategoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == request.CategoryId.Value);
            }

            if (request.BrandId.HasValue)
            {
                query = query.Where(p => p.BrandId == request.BrandId.Value);
            }

            if (request.StockFrom.HasValue)
            {
                query = query.Where(p => p.Stock >= request.StockFrom.Value);
            }

            if (request.StockTo.HasValue)
            {
                query = query.Where(p => p.Stock <= request.StockTo.Value);
            }

            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 25 : Math.Min(request.PageSize, 100);
            var orderedQuery = query.OrderByDescending(p => p.ProductId);
            var totalCount = await orderedQuery.CountAsync();
            var products = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            var pagedProducts = new StaticPagedList<Product>(products, page, pageSize, totalCount);

            return new AdminProductIndexData
            {
                Products = pagedProducts,
                Categories = await _context.Categories.ToListAsync(),
                Brands = await _context.Brands.ToListAsync(),
                Filters = request,
                PageSize = pageSize
            };
        }

        public async Task<List<ProductViewModel>> FilterAsync(AdminProductFilterRequest request)
        {
            var products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrEmpty(request.Sku))
                products = products.Where(p => p.Sku.Contains(request.Sku.Trim()));
            if (!string.IsNullOrEmpty(request.Name))
                products = products.Where(p => p.Name.Contains(request.Name.Trim()));
            if (!string.IsNullOrEmpty(request.Status))
                products = products.Where(p => p.Status == request.Status);
            if (request.CategoryId.HasValue)
                products = products.Where(p => p.CategoryId == request.CategoryId.Value);
            if (request.BrandId.HasValue)
                products = products.Where(p => p.BrandId == request.BrandId.Value);
            if (request.StockFrom.HasValue)
                products = products.Where(p => p.Stock >= request.StockFrom.Value);
            if (request.StockTo.HasValue)
                products = products.Where(p => p.Stock <= request.StockTo.Value);

            return await products
                .OrderByDescending(p => p.ProductId)
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
                .ToListAsync();
        }

        public async Task<AdminProductLookupData> GetLookupDataAsync()
        {
            return new AdminProductLookupData
            {
                Categories = await _context.Categories
                    .Select(x => new Category
                    {
                        CategoryId = x.CategoryId,
                        Name = x.Name
                    })
                    .ToListAsync(),
                Brands = await _context.Brands
                    .Select(x => new Brand
                    {
                        BrandId = x.BrandId,
                        Name = x.Name
                    })
                    .ToListAsync(),
                Attributes = await _context.Attributes
                    .Include(p => p.AttributeValues)
                    .Select(x => new Models.Attribute
                    {
                        AttributeId = x.AttributeId,
                        Name = x.Name,
                        Code = x.Code,
                        AttributeValues = x.AttributeValues,
                        SortOrder = x.SortOrder
                    })
                    .OrderBy(x => x.SortOrder)
                    .ToListAsync()
            };
        }

        public async Task<AdminProductEditData?> GetEditDataAsync(int id)
        {
            var product = await _context.Products
                .Include(p => p.VarientProducts)
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.Galleries)
                .FirstOrDefaultAsync(x => x.ProductId == id);

            if (product == null)
            {
                return null;
            }

            var variantIds = await _context.VarientProducts
                .Where(x => x.ProductId == product.ProductId)
                .Select(x => x.VarientId)
                .ToListAsync();

            var productAttributes = await _context.VariantAttributes
                .Where(pa => variantIds.Contains(pa.ProductVariantId))
                .Include(pa => pa.AttributeValue)
                .Select(pa => pa.AttributeValue.AttributeId)
                .Distinct()
                .ToListAsync();

            var lookup = await GetLookupDataAsync();

            return new AdminProductEditData
            {
                Product = product,
                CheckedAttributeIds = productAttributes,
                Categories = lookup.Categories,
                Brands = lookup.Brands,
                Attributes = lookup.Attributes
            };
        }

        public async Task<AdminProductActionResult> CreateAsync(ProductDTo productDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = new Product
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
                };

                var imageFile = productDto.Image;
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = $"SP_{product.Sku}.webp";
                    product.Image = await SaveProductFileAsync(imageFile, fileName);
                }
                else
                {
                    product.Image = "none.jpg";
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                if (productDto.VarientProducts != null && productDto.VarientProducts.Any())
                {
                    foreach (var variantDto in productDto.VarientProducts)
                    {
                        var imageFileName = $"SP_{product.Sku}_{variantDto.Sku}.webp";
                        string? imageUrl = null;

                        if (variantDto.Image != null)
                        {
                            imageUrl = await SaveProductFileAsync(variantDto.Image, imageFileName);
                        }

                        var variant = new VarientProduct
                        {
                            ProductId = product.ProductId,
                            Sku = variantDto.Sku,
                            Attributes = variantDto.Attributes,
                            Stock = variantDto.Stock ?? 0,
                            Price = variantDto.Price ?? product.SellPrice,
                            ImageUrl = variantDto.Image != null ? imageUrl : product.Image
                        };

                        _context.VarientProducts.Add(variant);
                        await _context.SaveChangesAsync();

                        if (productDto.IsUseVariant == true)
                        {
                            await AddVariantAttributesAsync(variant.VarientId, variantDto.Attributes);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    var defaultVariant = new VarientProduct
                    {
                        ProductId = product.ProductId,
                        Sku = product.Sku,
                        Attributes = "",
                        Stock = product.Stock,
                        Price = product.SellPrice,
                        ImageUrl = product.Image
                    };

                    _context.VarientProducts.Add(defaultVariant);
                    await _context.SaveChangesAsync();
                }

                if (productDto.Galleries != null && productDto.Galleries.Any())
                {
                    await AddGalleryImagesAsync(product, productDto.Galleries);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();
                await GenerateProductsJsonAsync();

                await _notificationService.NotifyAsync(
                    Events.NotificationTarget.Admins,
                    "Sản phẩm mới",
                    $"Sản phẩm {product.Name} được thêm vào danh sách",
                    "product added",
                    $"/admin/products/views/{product.ProductId}");

                return new AdminProductActionResult { Success = true };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new AdminProductActionResult
                {
                    Success = false,
                    Message = "Đã xảy ra lỗi trong quá trình lưu trữ: " + ex.Message
                };
            }
        }

        public async Task<AdminProductActionResult> UpdateAsync(ProductDTo productDto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var product = await _context.Products.FindAsync(productDto.ProductId);
                if (product == null)
                {
                    return new AdminProductActionResult { NotFound = true, Message = "Không tìm thấy sản phẩm" };
                }

                if (productDto.Image != null)
                {
                    await UpdateMainImageAsync(product, productDto.Image);
                }

                UpdateProductCoreFields(product, productDto);
                await ProcessVariantsAsync(productDto);

                if (productDto.Galleries != null)
                {
                    await AddGalleryImagesAsync(product, productDto.Galleries);
                }

                product.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await GenerateProductsJsonAsync();
                return new AdminProductActionResult { Success = true };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new AdminProductActionResult
                {
                    Success = false,
                    Message = "Có lỗi xảy ra khi cập nhật"
                };
            }
        }

        public async Task<AdminProductActionResult> ChangeVisibleAsync(int productId)
        {
            if (productId == 0)
            {
                return new AdminProductActionResult { Success = false, Message = "Không nhận được mã sản phẩm" };
            }

            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == productId);
            if (product == null)
            {
                return new AdminProductActionResult { Success = false, Message = "Không tìm thấy sản phẩm" };
            }

            product.Visible = product.Visible == false ? true : false;
            await _context.SaveChangesAsync();

            return new AdminProductActionResult
            {
                Success = true,
                Message = "Thay đổi thành công",
                Visible = product.Visible
            };
        }

        public async Task<AdminProductActionResult> DeleteAsync(int id)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return new AdminProductActionResult { Success = false, NotFound = true, Message = "Không tìm thấy sản phẩm" };
            }

            var variantProducts = await _context.VarientProducts.Where(x => x.ProductId == id).ToListAsync();
            var variantProductIds = variantProducts.Select(x => x.VarientId).ToList();
            var galleries = await _context.Galleries.Where(x => x.ProductId == id).ToListAsync();
            var cartItems = await _context.CartItems.Where(x => x.ProductId == id).ToListAsync();
            var reviews = await _context.Reviews.Where(x => x.ProductId == id).ToListAsync();
            var wishlists = await _context.Wishlists.Where(x => x.ProductId == id).ToListAsync();
            var orderItems = await _context.OrderItems.Where(x => x.ProductId == id).ToListAsync();
            var variantProductAttrs = await _context.VariantAttributes
                .Where(x => variantProductIds.Contains(x.ProductVariantId))
                .ToListAsync();

            if (orderItems.Any())
            {
                product.Status = "discontinued";
                product.Visible = false;
                await _context.SaveChangesAsync();
                return new AdminProductActionResult
                {
                    Success = true,
                    Message = "Sản phẩm đã được giao dịch, chỉ có thể Khóa/Ẩn"
                };
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (variantProductAttrs.Any())
                {
                    _context.VariantAttributes.RemoveRange(variantProductAttrs);
                    await _context.SaveChangesAsync();
                }

                if (galleries.Any()) _context.Galleries.RemoveRange(galleries);
                if (cartItems.Any()) _context.CartItems.RemoveRange(cartItems);
                if (reviews.Any()) _context.Reviews.RemoveRange(reviews);
                if (wishlists.Any()) _context.Wishlists.RemoveRange(wishlists);

                await _context.SaveChangesAsync();

                if (variantProducts.Any())
                {
                    foreach (var variant in variantProducts)
                    {
                        DeletePhysicalFile(variant.ImageUrl);
                    }

                    _context.VarientProducts.RemoveRange(variantProducts);
                    await _context.SaveChangesAsync();
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                await GenerateProductsJsonAsync();
                await transaction.CommitAsync();

                DeletePhysicalFile(product.Image);
                foreach (var gallery in galleries)
                {
                    DeletePhysicalFile(gallery.Path);
                }

                return new AdminProductActionResult
                {
                    Success = true,
                    Message = "Sản phẩm đã được xóa thành công khỏi danh sách."
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new AdminProductActionResult
                {
                    Success = false,
                    Message = "Lỗi khi xóa sản phẩm: " + ex.Message
                };
            }
        }

        public async Task<AdminProductActionResult> DeleteMainImageAsync(DeleteFileViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FilePath))
                    return new AdminProductActionResult { Success = false, Message = "Đường dẫn file không hợp lệ" };

                var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == model.ProductId);
                if (product?.Image != null)
                {
                    DeletePhysicalFile(product.Image);
                    product.Image = null;
                    await _context.SaveChangesAsync();
                    return new AdminProductActionResult { Success = true };
                }

                return new AdminProductActionResult { Success = false, Message = "Ảnh không tồn tại" };
            }
            catch
            {
                return new AdminProductActionResult { Success = false, Message = "Đã có lỗi xảy ra" };
            }
        }

        public async Task<AdminProductActionResult> DeleteGalleryImageAsync(DeleteFileViewModel model)
        {
            try
            {
                if (string.IsNullOrEmpty(model.FilePath))
                    return new AdminProductActionResult { Success = false, Message = "Đường dẫn file không hợp lệ" };

                var img = await _context.Galleries
                    .FirstOrDefaultAsync(x => x.ProductId == model.ProductId && x.Path.Contains(model.FilePath));

                if (img == null)
                {
                    return new AdminProductActionResult { Success = false, NotFound = true, Message = "Không tìm thấy ảnh thư viện" };
                }

                DeletePhysicalFile(model.FilePath);
                _context.Galleries.Remove(img);
                await _context.SaveChangesAsync();

                return new AdminProductActionResult { Success = true };
            }
            catch
            {
                return new AdminProductActionResult { Success = false, Message = "Đã có lỗi xảy ra" };
            }
        }

        public async Task<AdminProductCodeData?> GetCodeDataAsync(int id, string? content, string? codeType, string scheme)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return null;
            }

            var resolvedContent = content != null && content == "url"
                ? $"/Home/View/{id}"
                : product.Sku;

            var result = new AdminProductCodeData
            {
                Product = product,
                Content = content,
                CodeType = codeType
            };

            if (string.IsNullOrEmpty(codeType) || codeType == "QRCode")
            {
                result.QRCodeImage = GenerateQRCode(resolvedContent);
            }
            else if (codeType == "Barcode")
            {
                result.BarcodeImage = GenerateBarcode(product.Sku);
            }

            await Task.Yield();
            return result;
        }

        public async Task<AdminProductActionResult> BuildPrintCodeAsync(int id, string content, string codeType, int quantity)
        {
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return new AdminProductActionResult { Success = false, NotFound = true, Message = "Product not found" };
            }

            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Template", "print-code-template.html");
            string templateHtml = await File.ReadAllTextAsync(path);

            var replace = new StringBuilder(templateHtml);
            var codeImages = new StringBuilder();

            for (int i = 1; i <= quantity; i++)
            {
                string codeImage = codeType == "Barcode" ? GenerateBarcode(product.Sku) : GenerateQRCode(content);

                codeImages.AppendLine("<div>");
                codeImages.AppendLine($"<img src='{codeImage}' alt='Generated Code' />");
                codeImages.AppendLine($"<p> {product.Name}</p>");
                codeImages.AppendLine($"<p>{product.Sku}</p>");
                codeImages.AppendLine("</div>");
            }

            replace.Replace("@@CodeImage", codeImages.ToString());
            replace.Replace("@@ProductName", product.Name);
            replace.Replace("@@ProductSku", product.Sku);

            return new AdminProductActionResult
            {
                Success = true,
                Html = replace.ToString()
            };
        }

        private async Task UpdateMainImageAsync(Product product, IFormFile newImage)
        {
            var fileName = $"SP_{product.Sku}_{Guid.NewGuid()}.webp";
            DeletePhysicalFile(product.Image);
            product.Image = await SaveProductFileAsync(newImage, fileName);
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
            product.DiscountAmount = dto.DiscountAmount;
            product.DiscountPercentage = dto.DiscountAmount.HasValue ? null : dto.DiscountPercentage;
        }

        private async Task ProcessVariantsAsync(ProductDTo dto)
        {
            if (dto.VarientProducts == null) return;

            var existingVariants = await _context.VarientProducts
                .Where(v => v.ProductId == dto.ProductId)
                .ToListAsync();

            var variantSkus = dto.VarientProducts.Select(v => v.Sku).ToHashSet();
            var removedVariants = existingVariants.Where(ev => !variantSkus.Contains(ev.Sku)).ToList();
            var removedVariantIds = removedVariants.Select(v => v.VarientId).ToList();

            var variantAttributesToRemove = await _context.VariantAttributes
                .Where(va => removedVariantIds.Contains(va.ProductVariantId))
                .ToListAsync();

            _context.VariantAttributes.RemoveRange(variantAttributesToRemove);

            var referencedVariantIds = await _context.OrderItems
                .Where(oi => removedVariantIds.Contains(oi.VarientProductId))
                .Select(oi => oi.VarientProductId)
                .ToListAsync();

            var safeToDelete = removedVariants.Where(v => !referencedVariantIds.Contains(v.VarientId)).ToList();
            _context.VarientProducts.RemoveRange(safeToDelete);

            foreach (var variantDto in dto.VarientProducts)
            {
                var existing = existingVariants.FirstOrDefault(v => v.Sku == variantDto.Sku);
                var newImageFileName = $"SP_{dto.Sku}_{variantDto.Sku}_{Guid.NewGuid()}.webp";
                string? imageUrl = null;

                if (variantDto.Image is IFormFile newImage && newImage.Length > 0)
                {
                    if (existing != null && !string.IsNullOrEmpty(existing.ImageUrl))
                    {
                        DeletePhysicalFile(existing.ImageUrl);
                    }

                    imageUrl = await SaveProductFileAsync(newImage, newImageFileName);
                }

                if (existing != null)
                {
                    existing.Attributes = variantDto.Attributes;
                    existing.Stock = variantDto.Stock ?? 0;
                    existing.Price = variantDto.Price ?? dto.SellPrice ?? 0;
                    existing.ImageUrl = variantDto.Image != null ? imageUrl : existing.ImageUrl;
                }
                else
                {
                    var imagePathDefault = await _context.Products
                        .Where(x => x.ProductId == dto.ProductId)
                        .Select(x => x.Image)
                        .FirstOrDefaultAsync();

                    _context.VarientProducts.Add(new VarientProduct
                    {
                        ProductId = dto.ProductId,
                        Sku = variantDto.Sku,
                        Attributes = variantDto.Attributes,
                        Stock = variantDto.Stock ?? 0,
                        Price = variantDto.Price ?? dto.SellPrice ?? 0,
                        ImageUrl = variantDto.Image != null ? imageUrl : imagePathDefault
                    });
                }
            }

            await _context.SaveChangesAsync();
            await ProcessVariantAttributesAsync(dto);
        }

        private async Task ProcessVariantAttributesAsync(ProductDTo dto)
        {
            if (dto.IsUseVariant != true) return;

            var variants = await _context.VarientProducts
                .Where(v => v.ProductId == dto.ProductId)
                .ToListAsync();

            var variantIds = variants.Select(v => v.VarientId).ToList();
            var existingAttributes = await _context.VariantAttributes
                .Where(va => variantIds.Contains(va.ProductVariantId))
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
                    if (attribute == null) continue;

                    var value = await _context.AttributeValues
                        .FirstOrDefaultAsync(av => av.AttributeId == attribute.AttributeId
                            && av.Value == parts[1].Trim());
                    if (value == null) continue;

                    var existingVariantAttribute = await _context.VariantAttributes
                        .FirstOrDefaultAsync(va => va.ProductVariantId == variant.VarientId
                            && va.AttributeValueId == value.AttributeValueId);

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

        private async Task AddVariantAttributesAsync(int variantId, string? attributes)
        {
            if (string.IsNullOrWhiteSpace(attributes)) return;

            var attributePairs = attributes.Split(',');
            foreach (var attributePair in attributePairs)
            {
                var attrParts = attributePair.Split(':');
                if (attrParts.Length != 2) continue;

                var attr = attrParts[0].Trim();
                var attrValue = attrParts[1].Trim();

                var attrQuery = await _context.Attributes.FirstOrDefaultAsync(x => x.Code == attr);
                if (attrQuery == null) continue;

                var attrValueQuery = await _context.AttributeValues
                    .FirstOrDefaultAsync(x => x.Value == attrValue && x.AttributeId == attrQuery.AttributeId);
                if (attrValueQuery == null) continue;

                _context.VariantAttributes.Add(new VariantAttribute
                {
                    ProductVariantId = variantId,
                    AttributeValueId = attrValueQuery.AttributeValueId,
                });
            }
        }

        private async Task AddGalleryImagesAsync(Product product, IEnumerable<IFormFile> images)
        {
            foreach (var image in images)
            {
                if (image.Length == 0) continue;

                var fileName = $"GSP_{product.Sku}_{Guid.NewGuid()}.webp";
                await SaveProductFileAsync(image, fileName);

                _context.Galleries.Add(new Gallery
                {
                    ProductId = product.ProductId,
                    Path = fileName
                });
            }
        }

        private async Task<string> SaveProductFileAsync(IFormFile file, string fileName)
        {
            var uploadRoot = Path.Combine(_webHostEnvironment.WebRootPath, "Upload", "Products");
            Directory.CreateDirectory(uploadRoot);
            var fullPath = Path.Combine(uploadRoot, fileName);

            await using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            return fileName;
        }

        private void DeletePhysicalFile(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName == "none.jpg") return;

            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, "Upload", "Products", fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }

        private string GenerateQRCode(string content)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            PngByteQRCode qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(20);
            return "data:image/png;base64," + Convert.ToBase64String(qrCodeBytes);
        }

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
            using var barcodeBitmap = new System.Drawing.Bitmap(
                pixelData.Width,
                pixelData.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            using var barcodeStream = new MemoryStream();

            var bitmapData = barcodeBitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, pixelData.Width, pixelData.Height),
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

        public static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            string slug = RemoveDiacritics(name.ToLower());
            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", "-").Trim();
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

        private async Task GenerateProductsJsonAsync()
        {
            var productsList = await _context.Products
                .Select(x => new
                {
                    x.ProductId,
                    x.Name,
                    x.Slug,
                    x.Image,
                    x.SellPrice,
                    x.OriginalPrice
                })
                .ToListAsync();

            var path = Path.Combine(_webHostEnvironment.WebRootPath, "json");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            var filePath = Path.Combine(path, "products.json");
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(productsList));
        }
    }
}
