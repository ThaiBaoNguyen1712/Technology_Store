using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class BrandsController : BaseAdminController
    {
        private readonly IBrandService _brandService;
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;

        public BrandsController(ApplicationDbContext context, IBrandService brandService, IConfiguration configuration) : base(context)
        {
            _brandService = brandService;
            _configuration = configuration;
        }

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            return pageSize.GetValueOrDefault(FallbackPageSize) > 0 ? pageSize.Value : FallbackPageSize;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? keyword, int? categoryId, int page = 1, int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var model = await _brandService.GetAdminBrandIndexAsync(keyword, categoryId, page, resolvedPageSize);
            return View(model);
        }

        [HttpGet("Form")]
        public async Task<IActionResult> Form(int? id, int page = 1, int? pageSize = null, string? keyword = null, int? categoryId = null)
        {
            var model = await _brandService.GetBrandFormAsync(id);
            if (id.HasValue && model == null)
            {
                return NotFound();
            }

            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var viewModel = model ?? new AdminBrandFormViewModel();
            viewModel.ReturnPage = Math.Max(1, page);
            viewModel.ReturnPageSize = resolvedPageSize;
            viewModel.ReturnKeyword = keyword;
            viewModel.ReturnCategoryId = categoryId;

            ViewBag.Categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

            return View(viewModel);
        }

        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AdminBrandFormViewModel model, IFormFile? imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await _context.Categories
                    .AsNoTracking()
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                return View("Form", model);
            }

            await _brandService.SaveBrandAsync(model, imageFile);
            return RedirectToAction(nameof(Index), new
            {
                page = model.ReturnPage,
                pageSize = model.ReturnPageSize,
                keyword = model.ReturnKeyword,
                categoryId = model.ReturnCategoryId
            });
        }

        [HttpDelete("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _brandService.DeleteBrandAsync(id);
            if (!result)
            {
                return NotFound("Không tìm thấy brand với ID đã cho");
            }

            return Ok();
        }
    }
}
