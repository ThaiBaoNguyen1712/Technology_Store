using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class CategoriesController : BaseAdminController
    {
        private readonly ICategoryService _categoryService;
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;

        public CategoriesController(ApplicationDbContext context, ICategoryService categoryService, IConfiguration configuration) : base(context)
        {
            _categoryService = categoryService;
            _configuration = configuration;
        }

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            return pageSize.GetValueOrDefault(FallbackPageSize) > 0 ? pageSize.Value : FallbackPageSize;
        }

        [HttpGet]
        public IActionResult Index(string? keyword, int page = 1, int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var query = _context.Categories.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    x.Name.Contains(normalizedKeyword) ||
                    (x.EngTitle != null && x.EngTitle.Contains(normalizedKeyword)) ||
                    (x.Description != null && x.Description.Contains(normalizedKeyword)));
            }

            var totalItems = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)resolvedPageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            var model = new AdminCategoryIndexViewModel
            {
                Keyword = keyword,
                Page = page,
                PageSize = resolvedPageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Categories = query
                    .OrderBy(x => x.SortOrder)
                    .ThenBy(x => x.Name)
                    .ThenBy(x => x.CategoryId)
                    .Skip((page - 1) * resolvedPageSize)
                    .Take(resolvedPageSize)
                    .Select(x => new AdminCategoryIndexItemViewModel
                    {
                        CategoryId = x.CategoryId,
                        Name = x.Name,
                        EngTitle = x.EngTitle,
                        Image = x.Image,
                        Description = x.Description,
                        Visible = x.Visible,
                        SortOrder = x.SortOrder
                    })
                    .ToList()
            };

            return View(model);
        }

        [HttpGet("Form")]
        public IActionResult Form(int? id, int page = 1, int? pageSize = null, string? keyword = null)
        {
            AdminCategoryFormViewModel model;
            var categories = _context.Categories.ToList();
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());

            if (id == null)
            {
                model = new AdminCategoryFormViewModel();
            }
            else
            {
                model = _context.Categories
                    .Where(x => x.CategoryId == id)
                    .Select(x => new AdminCategoryFormViewModel
                    {
                        CategoryId = x.CategoryId,
                        ParentId = x.ParentId,
                        Name = x.Name,
                        EngTitle = x.EngTitle ?? string.Empty,
                        Image = x.Image,
                        Description = x.Description,
                        Visible = x.Visible,
                        VisibleOnCategoryPage = x.VisibleOnCategoryPage,
                        VisibleOnOtherPages = x.VisibleOnOtherPages,
                        SortOrder = x.SortOrder
                    })
                    .FirstOrDefault() ?? throw new InvalidOperationException();

                categories = categories.Where(x => x.CategoryId != id).ToList();
            }

            model.ReturnPage = Math.Max(1, page);
            model.ReturnPageSize = resolvedPageSize;
            model.ReturnKeyword = keyword;

            ViewBag.ParentCategories = categories;
            return View(model);
        }

        [HttpPost("Save")]
        public async Task<IActionResult> Save([FromForm] AdminCategoryFormViewModel model, IFormFile? imageFile, string categoryType)
        {
            var cate = new Category
            {
                CategoryId = model.CategoryId,
                ParentId = model.ParentId,
                Name = model.Name,
                EngTitle = model.EngTitle,
                Image = model.Image,
                Description = model.Description,
                Visible = model.Visible,
                VisibleOnCategoryPage = model.VisibleOnCategoryPage,
                VisibleOnOtherPages = model.VisibleOnOtherPages,
                SortOrder = model.SortOrder
            };

            if (categoryType == "root")
            {
                cate.ParentId = null;
            }

            if (cate.CategoryId == 0)
            {
                await _categoryService.CreateCategoryAsync(cate, imageFile);
            }
            else
            {
                var result = await _categoryService.UpdateCategoryAsync(cate.CategoryId, cate, imageFile);
                if (!result)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy danh mục" });
                }
            }

            return RedirectToAction("Index", new
            {
                page = model.ReturnPage,
                pageSize = model.ReturnPageSize,
                keyword = model.ReturnKeyword
            });
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

        [HttpPost("ChangeVisible")]
        public async Task<JsonResult> ChangeVisible(int id)
        {
            var result = await _categoryService.ChangeVisible(id);
            if (!result)
            {
                return Json(new { success = false, message = "Không tìm thấy danh mục" });
            }

            return Json(new { success = true, message = "Thay đổi thành công", visible = result });
        }
    }
}
