using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class CategoriesController : BaseAdminController
    {
   
        public CategoriesController(ApplicationDbContext context) : base(context) { }
      
        [HttpGet]
        public IActionResult Index()
        {
            var list_cate = _context.Categories.ToList();
            return View(list_cate);
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm]Category cate,IFormFile? imageFile)
        {
            // Lưu hình ảnh vào file
            if (imageFile.Length > 0 && imageFile != null)
            {
                var fileName = $"Cate_{Guid.NewGuid()}.png";
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                cate.Image = fileName;
            }
            else
            {
                cate.Image = "none.jpg";
            }
            _context.Categories.Add(cate);
            await _context.SaveChangesAsync();
            return Ok();
        }
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var cate = _context.Categories.FirstOrDefault(x=>x.CategoryId==id);
            if(cate ==null)
            {
                return NotFound();
            }
            return Json(cate);
        }
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] Category cate, IFormFile? imageFile)
        {
            try
            {
                var category = await _context.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
                if (category == null)
                {
                    return NotFound();
                }

                // Kiểm tra và xóa ảnh cũ nếu có
                if (!string.IsNullOrEmpty(category.Image) && category.Image != "none.jpg")
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo", category.Image);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                // Lưu hình ảnh mới nếu có
                if (imageFile != null && imageFile.Length > 0)
                {
                    var fileName = $"Cate_{Guid.NewGuid()}.png";
                    var newImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(newImagePath)!);

                    using (var stream = new FileStream(newImagePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    // Cập nhật tên file vào thuộc tính Image của `category`
                    category.Image = fileName;
                }

                // Cập nhật các thuộc tính khác
                category.Name = cate.Name;
                category.Description = cate.Description;
                category.Visible = cate.Visible;

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
                var cate = await _context.Categories.FindAsync(id); // Tìm danh mục theo ID
                if (cate == null)
                {
                    return BadRequest();
                }

                _context.Categories.Remove(cate);
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch(Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpPost("ChangeVisible")]
        public async Task<JsonResult> ChangeVisible(int id)
        {
            if (id == 0)
            {
                return Json(new { success = false, message = "Không tìm thấy ID danh mục" });
            }

            var cate = await _context.Categories.FirstOrDefaultAsync(x => x.CategoryId == id);
            if (cate == null)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục" });
            }

            // Đảo ngược giá trị Visible
            cate.Visible = cate.Visible == 0 ? 1 : 0;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thay đổi thành công", visible = cate.Visible });
        }

    }
}
