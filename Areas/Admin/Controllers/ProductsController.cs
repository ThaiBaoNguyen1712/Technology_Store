using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
using static System.Net.Mime.MediaTypeNames;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]

    public class ProductsController : BaseAdminController
    {
        private readonly IConfiguration _configuration;

        public ProductsController(ApplicationDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
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

        [Route("View/{id}")]
        public async Task<IActionResult> View(int id)
        {
            
            var detail = await _context.Products.Include(x=> x.Category).Include(x=> x.Brand)
                .Include(x=>x.Galleries).Include(x=>x.Reviews).Include(x=>x.VarientProducts).
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
            IQueryable<Product> query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .OrderByDescending(p => p.ProductId);

            // Nếu status không phải là null và không phải là "all", lọc theo trạng thái
            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(p => p.Status == status);
            }

            // Lấy danh sách sản phẩm từ cơ sở dữ liệu
            var list_products = await query.ToListAsync();

            return View(list_products);
        }

        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            ViewBag.Categories = GetListCategories();
            ViewBag.Brands = GetListBrand();

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
                        }
                        await _context.SaveChangesAsync();
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
            ViewBag.Categories = GetListCategories();
            ViewBag.Brands = GetListBrand();
            return View(_product);
        }
        [HttpPost]
        [Route("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProductDTo productDto, List<IFormFile> gallery)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid model state");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingProduct = await _context.Products.FindAsync(productDto.ProductId);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                if (productDto.Image != null)
                {
                    await UpdateProductImage(existingProduct, productDto);
                }

                UpdateProductDetails(existingProduct, productDto);
                await UpdateProductVariants(productDto);
                if(productDto.Galleries != null)
                {
                    await UpdateProductGallery(existingProduct, (List<IFormFile>)productDto.Galleries);

                }

                existingProduct.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                await GenerateProductsJson();
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Consider logging the exception
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }
        }

        [HttpPost("ChangeVisible")]
        public async Task<JsonResult> ChangeVisible(int productId)
        {
            if(productId ==  0)
            {
                return Json(new { success = false, message = "Không nhận được mã sản phẩm" });
            }
            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId.Equals(productId));
            if(product == null)
            {

                return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
            }
            // Đảo ngược giá trị Visible
            product.Visible = product.Visible == false ? true : false;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thay đổi thành công", visible = product.Visible });
        }

        private async Task UpdateProductImage(Product existingProduct, ProductDTo productDto)
        {
            // Kiểm tra nếu có hình ảnh mới
            if (productDto.Image != null && productDto.Image.Length > 0)
            {
                // Xóa hình ảnh cũ nếu có
                if (!string.IsNullOrEmpty(existingProduct.Image))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", existingProduct.Image);
                    if (System.IO.File.Exists(oldImagePath)) // Sử dụng System.IO.File
                    {
                        try
                        {
                            System.IO.File.Delete(oldImagePath); // Xóa hình ảnh cũ
                        }
                        catch (Exception ex)
                        {
                            // Xử lý lỗi nếu cần (ghi log hoặc thông báo cho người dùng)
                            Console.WriteLine($"Không thể xóa hình ảnh cũ: {ex.Message}");
                        }
                    }
                }

                // Tạo tên file mới và lưu hình ảnh mới
                var fileName = $"SP_{productDto.Sku}.webp";
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                // Lưu hình ảnh mới vào ổ đĩa
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await productDto.Image.CopyToAsync(stream);
                }

                existingProduct.Image = fileName; // Cập nhật hình ảnh mới
            }
        }



        private void UpdateProductDetails(Product existingProduct, ProductDTo updatedProduct)
        {
            existingProduct.Name = updatedProduct.Name;
            existingProduct.Description = updatedProduct.Description;
            existingProduct.OriginalPrice = updatedProduct.OriginalPrice;
            existingProduct.CostPrice = updatedProduct.CostPrice;
            existingProduct.SellPrice = updatedProduct.SellPrice;
            existingProduct.WarrantyPeriod = updatedProduct.WarrantyPeriod;
            existingProduct.Status = updatedProduct.Status;
            existingProduct.BrandId = updatedProduct.BrandId;
            existingProduct.Stock = updatedProduct.Stock;
            existingProduct.Weight = updatedProduct.Weight;
            existingProduct.CategoryId = updatedProduct.CategoryId;
            existingProduct.UrlYoutube = updatedProduct.UrlYoutube;
            existingProduct.Sku = updatedProduct.Sku;
            existingProduct.Color = updatedProduct.Color;
            existingProduct.Slug = GenerateSlug(updatedProduct.Name);
            if (updatedProduct.DiscountAmount != null)
            {
                existingProduct.DiscountAmount = updatedProduct.DiscountAmount;
                existingProduct.DiscountPercentage = null;
            }
            else if(updatedProduct.DiscountAmount != null)
            {
                existingProduct.DiscountPercentage = updatedProduct.DiscountPercentage;
                existingProduct.DiscountAmount = null;
            }
        }

        private async Task UpdateProductVariants(ProductDTo productDto)
        {
            if (productDto.VarientProducts == null || !productDto.VarientProducts.Any())
            {
                return;
            }

            var existingVariants = await _context.VarientProducts
                .Where(v => v.ProductId == productDto.ProductId)
                .ToListAsync();

            foreach (var item in productDto.VarientProducts)
            {
                var existingVariant = existingVariants.FirstOrDefault(v => v.Sku == item.Sku);
                if (existingVariant != null)
                {
                    UpdateExistingVariant(existingVariant, item, productDto.SellPrice ?? 0);
                }
                else
                {
                    AddNewVariant(item, productDto);
                }
            }

            RemoveDeletedVariants(existingVariants, productDto.VarientProducts.ToList());
        }

        private void UpdateExistingVariant(VarientProduct existingVariant, VarientProduct updatedVariant, decimal productSellPrice)
        {
            existingVariant.Attributes = updatedVariant.Attributes;
            existingVariant.Stock = updatedVariant.Stock ?? 0;
            existingVariant.Price = updatedVariant.Price ?? productSellPrice;
        }

        private void AddNewVariant(VarientProduct newVariant, ProductDTo productDto)
        {
            newVariant.Stock = newVariant.Stock ?? 0;
            newVariant.ProductId = productDto.ProductId;
            newVariant.Price = newVariant.Price ?? productDto.SellPrice ?? 0;
            _context.VarientProducts.Add(newVariant);
        }

        private void RemoveDeletedVariants(List<VarientProduct> existingVariants, List<VarientProduct> updatedVariants)
        {
            var removedVariants = existingVariants
                .Where(v => !updatedVariants.Any(p => p.Sku == v.Sku))
                .ToList();
            _context.VarientProducts.RemoveRange(removedVariants);
        }

        private async Task UpdateProductGallery(Product product, List<IFormFile> gallery)
        {
            if (gallery == null || gallery.Count == 0)
            {
                return;
            }

            var existingGallery = await _context.Galleries
                .Where(g => g.ProductId == product.ProductId)
                .ToListAsync();

            RemoveDeletedGalleryImages(existingGallery, gallery);
            await AddNewGalleryImages(product, gallery, existingGallery);
        }

        private void RemoveDeletedGalleryImages(List<Gallery> existingGallery, List<IFormFile> updatedGallery)
        {
            var removedGallery = existingGallery
                .Where(g => !updatedGallery.Any(newImg => newImg.FileName == g.Path))
                .ToList();

            foreach (var removed in removedGallery)
            {
                var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", removed.Path);
                if (System.IO.File.Exists(galleryImagePath))
                {
                    System.IO.File.Delete(galleryImagePath);
                }
                _context.Galleries.Remove(removed);
            }
        }

        private async Task AddNewGalleryImages(Product product, List<IFormFile> gallery, List<Gallery> existingGallery)
        {
            foreach (var item in gallery)
            {
                if (item.Length > 0 && !existingGallery.Any(g => g.Path == item.FileName))
                {
                    var galleryFileName = $"GSP_{product.Sku}_{Guid.NewGuid()}.webp";
                    var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", galleryFileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(galleryImagePath));

                    using (var stream = new FileStream(galleryImagePath, FileMode.Create))
                    {
                        await item.CopyToAsync(stream);
                    }

                    _context.Galleries.Add(new Gallery
                    {
                        ProductId = product.ProductId,
                        Path = galleryFileName
                    });
                }
            }
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

            if(product.Image  != null)
            {
                // Lưu đường dẫn hình ảnh của sản phẩm trước khi xóa
                var productImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", product.Image);
                // Gỡ bỏ tệp hình ảnh sản phẩm
                if (System.IO.File.Exists(productImagePath))
                {
                    System.IO.File.Delete(productImagePath); // Xóa file ảnh sản phẩm khỏi thư mục
                }
            }
          

            // Lấy tất cả các bản ghi liên quan
            var variantProducts = await _context.VarientProducts.Where(x => x.ProductId == id).ToListAsync();
            var galleries = await _context.Galleries.Where(x => x.ProductId == id).ToListAsync();
            var cartItems = await _context.CartItems.Where(x => x.ProductId == id).ToListAsync();
            var reviews = await _context.Reviews.Where(x => x.ProductId == id).ToListAsync();
            var wishlists = await _context.Wishlists.Where(x => x.ProductId == id).ToListAsync();
            var orderItems = await _context.OrderItems.Where(x => x.ProductId == id).ToListAsync();

            // Nếu sản phẩm đã được mua thì chỉ khóa lại
            if (orderItems.Any())
            {
                product.Status = "4"; // Khóa sản phẩm
            }
            else
            {
                // Xóa tất cả các liên kết nếu sản phẩm chưa được mua
                _context.VarientProducts.RemoveRange(variantProducts);
                _context.Galleries.RemoveRange(galleries);
                _context.CartItems.RemoveRange(cartItems);
                _context.Reviews.RemoveRange(reviews);
                _context.Wishlists.RemoveRange(wishlists);

               

                // Gỡ bỏ tất cả các tệp hình ảnh trong galleries
                foreach (var gallery in galleries)
                {
                    var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", gallery.Path);
                    if (System.IO.File.Exists(galleryImagePath))
                    {
                        System.IO.File.Delete(galleryImagePath); // Xóa từng file ảnh trong thư viện
                    }
                }
            }

            try
            {
                _context.Products.Remove(product);
                await GenerateProductsJson();
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }


        [HttpPost("DeleteImage/{id}")]
        public async Task<IActionResult> DeleteImage(int id) 
        {
            if (id <= 0)
            {
                return BadRequest("Tham số không hợp lệ");
            }

            var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            // Lưu đường dẫn ảnh trước khi xóa
            var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", product.Image);

            // Xóa ảnh trong DB
            product.Image = null;
            await _context.SaveChangesAsync();

            // Gỡ ảnh trong folder
            if (System.IO.File.Exists(galleryImagePath))
            {
                System.IO.File.Delete(galleryImagePath); 
            }

            return NoContent();
        }

        [HttpPost("DeleteImageFromGalleries/{id}")]
        public async Task<IActionResult> DeleteImageFromGalleries(int id) 
        {
            if (id <= 0)
            {
                return BadRequest("Tham số không hợp lệ");
            }

            var img = await _context.Galleries.FirstOrDefaultAsync(x => x.ImageId == id);
            if (img == null)
            {
                return NotFound("Không tìm thấy hình ảnh");
            }

            // Lưu đường dẫn ảnh trước khi xóa
            var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", img.Path);

            // Xóa ảnh trong DB
            _context.Galleries.Remove(img);
            await _context.SaveChangesAsync();

            // Gỡ ảnh trong folder
            if (System.IO.File.Exists(galleryImagePath))
            {
                System.IO.File.Delete(galleryImagePath);
            }

            return NoContent();
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

            // Tạo một số ngẫu nhiên (1000-9999)
            var random = new Random();
            int randomNumber = random.Next(1000, 9999);

            // Thêm số ngẫu nhiên vào Slug để đảm bảo không trùng lặp
            slug += "-" + randomNumber;

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
