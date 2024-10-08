using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]

    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductsController(ApplicationDbContext context)
        {
            _context = context;
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

        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var list_products = _context.Products.ToList();

            ViewBag.list_cate = GetListCategories();
            ViewBag.list_brands = GetListBrand();

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
        public async Task<IActionResult> Create(Product product, List<IFormFile> gallery)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Lưu hình ảnh chính của sản phẩm từ Request
                    var imageFile = Request.Form.Files.FirstOrDefault(); // Lấy tệp hình ảnh đầu tiên
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var fileName = $"SP_{Guid.NewGuid()}.png"; // Tạo tên tệp mới
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                        using (var stream = new FileStream(imagePath, FileMode.Create))
                        {
                            await imageFile.CopyToAsync(stream); // Lưu tệp hình ảnh vào thư mục
                        }

                        // Nếu bạn cần lưu tên tệp vào cơ sở dữ liệu, bạn có thể gán tên này cho product
                        product.Image = fileName; // Gán tên tệp hình ảnh cho sản phẩm
                    }
                    else
                    {
                        product.Image = "none.jpg"; // Gán hình ảnh mặc định nếu không có hình
                    }
                    //Phần khuyến mãi
                    if (product.DiscountedPrice > 0 || product.DiscountPercentage > 0)
                    {
                        product.IsDiscounted =true;
                    }
                    else
                    {
                        product.IsDiscounted=false;
                    }
                    product.Visible = true;
                    // Thêm sản phẩm vào cơ sở dữ liệu
                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Thêm danh sách hình ảnh vào bảng Gallery
                    if (gallery != null && gallery.Count > 0)
                    {
                        foreach (var item in gallery)
                        {
                            if (item.Length > 0) // Kiểm tra nếu có hình ảnh
                            {
                                var galleryFileName = $"GSP_{Guid.NewGuid()}.png"; // Tên tệp cho hình ảnh trong gallery
                                var galleryImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Products", galleryFileName);
                                Directory.CreateDirectory(Path.GetDirectoryName(galleryImagePath));

                                using (var stream = new FileStream(galleryImagePath, FileMode.Create))
                                {
                                    await item.CopyToAsync(stream); // Lưu hình ảnh vào thư mục
                                }

                                // Thêm hình ảnh vào bảng Gallery
                                var _gallery = new Gallery
                                {
                                    ProductId = product.ProductId,
                                    Path = galleryFileName // Gán đường dẫn hình ảnh
                                };

                                _context.Galleries.Add(_gallery);
                            }
                        }
                        await _context.SaveChangesAsync(); // Lưu tất cả hình ảnh vào cơ sở dữ liệu
                    }

                    // Commit transaction
                    await transaction.CommitAsync();
                    return RedirectToAction("Index"); // Chuyển hướng về trang danh sách sản phẩm
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi nếu có
                    await transaction.RollbackAsync(); // Rollback transaction nếu có lỗi
                    ModelState.AddModelError("", "Đã xảy ra lỗi trong quá trình lưu trữ: " + ex.Message);
                    return View("Error"); // Trả về trang lỗi
                }
            }
            return BadRequest("Invalid"); // Trả về lỗi nếu Model không hợp lệ
        }


        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var _product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == id);
            if (_product != null)
            {
                return NotFound();
            }
            ViewBag.Categories = GetListCategories();
            ViewBag.Brands = GetListBrand();
            return View(_product);
        }
        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            var _product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == product.ProductId);
            if (_product != null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                _context.Products.Update(product);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var _product = await _context.Products.FirstOrDefaultAsync(x=>x.ProductId == id);
            if (_product != null)
            {
                return BadRequest();
            }

            try
            {
                _context.Products.Remove(_product);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            
        }



    }
}
