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

        [HttpGet("Form")]
        public IActionResult Form(int? id)
        {
            Category model;

            var categories = _context.Categories.Where(x=>x.Visible == 1).ToList();
          
            if (id == null)
            {
                // CREATE
                model = new Category();
            }
            else
            {
                // EDIT
                model = _context.Categories.FirstOrDefault(x => x.CategoryId == id);
                categories = categories.Where(x=>x.CategoryId != id).ToList();
                if (model == null)
                    return NotFound();
            }
            ViewBag.ParentCategories = categories;
            return View(model);
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromForm] Category cate, IFormFile? imageFile ,string categoryType)
        {
            if (categoryType == "root")
            {
                cate.ParentId = null;
            }

            if (cate.CategoryId == 0)
            {
                // Create
                await _categoryService.CreateCategoryAsync(cate, imageFile);
            }
            else
            {
                // Update
                var result = await _categoryService.UpdateCategoryAsync(cate.CategoryId, cate, imageFile);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy danh mục" });
                }
            }
            return RedirectToAction("Index");
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var cate = await _categoryService.GetCategoryByIdAsync(id);
            return Json(cate);
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
