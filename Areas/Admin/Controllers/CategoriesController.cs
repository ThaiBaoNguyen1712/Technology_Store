using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Services.Admin.CategoryServices;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class CategoriesController : BaseAdminController
    {
    
        private readonly ICategoryService _categoryService;
        public CategoriesController(ApplicationDbContext context, ICategoryService categoryService) : base(context) {
            _categoryService = categoryService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var list_cate = _context.Categories.OrderByDescending(x=>x.CategoryId).ToList();
            return View(list_cate);
        }

        [Route("Create")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm]Category cate,IFormFile? imageFile)
        {
            if (cate == null)
            {
                return BadRequest("Dữ liệu danh mục không được trống");
            }
            await _categoryService.CreateCategoryAsync(cate, imageFile);
            return Ok();
        }
        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var cate = await _categoryService.GetCategoryByIdAsync(id);
            return Json(cate);
        }
        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] Category cate, IFormFile? imageFile)
        {
            var result = await _categoryService.UpdateCategoryAsync(id, cate, imageFile);
            if (!result)
            {
                return NotFound(new { success = false, message = "Không tìm thấy danh mục" });
            }
            return Ok(new { success = true, message = "Cập nhật thành công" });
        }


        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
           var result = _categoryService.DeleteCategoryAsync(id);
            if (!await result)
            {
                return NotFound();
            }
            return Ok(new { success = true, message = "Xóa thành công" });

        }
        // Controller
        [HttpPost("ChangeVisible")]
        public async Task<JsonResult> ChangeVisible(int id)
        {
            var result = await _categoryService.ChangeVisible(id);
            if (!result)
                return Json(new { success = false, message = "Không tìm thấy danh mục" });

            return Json(new { success = true, message = "Thay đổi thành công", visible = result });
        }

    }
}
