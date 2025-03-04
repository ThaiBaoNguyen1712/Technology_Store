using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class BrandsController : BaseAdminController
    {
     
        public BrandsController(ApplicationDbContext context) : base(context) { }
     

        [HttpGet]
        public IActionResult Index()
        {
            var list_cate = _context.Categories.ToList();
            ViewBag.cate = list_cate;
            var list_brands = _context.Brands.Include(p=>p.Category).ToList();
            return View(list_brands);
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm]Brand brand,IFormFile? imageFile)
        {   // Lưu hình ảnh vào file
            if (imageFile.Length > 0 && imageFile != null)
            {
                var fileName = $"Brand_{Guid.NewGuid()}.png";
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                brand.Image = fileName;
            }
            else
            {
                brand.Image = "none.jpg";
            }
            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var brand = _context.Brands.FirstOrDefault(x=>x.BrandId==id);
            if(brand == null)
            {
                return NotFound();
            }
            return Json(brand);
        }
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] Brand _brand,IFormFile? imageFile)
        {
            try
            {
                var brand = await _context.Brands.FirstOrDefaultAsync(x => x.BrandId == id);
                if (_brand == null)
                {
                    return NotFound();
                }
            

                // Lưu hình ảnh mới nếu có
                if (imageFile != null && imageFile.Length > 0)
                {
                    // Kiểm tra và xóa ảnh cũ nếu có
                    if (!string.IsNullOrEmpty(brand.Image) && brand.Image != "none.jpg")
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo", brand.Image);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                    var fileName = $"Brand_{Guid.NewGuid()}.png";
                    var newImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(newImagePath)!);

                    using (var stream = new FileStream(newImagePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Cập nhật tên file vào thuộc tính Image của `category`
                    brand.Image = fileName;
                }
                brand.Name = _brand.Name;
                brand.Description = _brand.Description;
                brand.CategoryId = _brand.CategoryId;
                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                // Ghi lại thông tin lỗi
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var brand = await _context.Brands.FindAsync(id); // Tìm danh mục theo ID
                if (brand == null)
                {
                    return BadRequest();
                }

                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
    }
}
