using Microsoft.AspNetCore.Mvc;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/[controller]")]
public class BannersController : BaseAdminController
{
    private readonly IBannerAdminService _bannerAdminService;
    private readonly IConfiguration _configuration;
    private const int FallbackPageSize = 20;

    public BannersController(ApplicationDbContext context, IBannerAdminService bannerAdminService, IConfiguration configuration) : base(context)
    {
        _bannerAdminService = bannerAdminService;
        _configuration = configuration;
    }

    private int GetDefaultAdminPageSize()
    {
        var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
        var resolvedPageSize = pageSize.GetValueOrDefault(FallbackPageSize);
        return resolvedPageSize > 0 ? resolvedPageSize : FallbackPageSize;
    }

    [HttpGet("")]
    [HttpGet("Index")]
    public async Task<IActionResult> Index(string? keyword, int? positionId, string? targetType, string? status, int page = 1)
    {
        var model = await _bannerAdminService.GetIndexAsync(keyword, positionId, targetType, status, page, GetDefaultAdminPageSize());
        return View(model);
    }

    [HttpGet("Form")]
    public async Task<IActionResult> Form(int? id, int page = 1, string? keyword = null, int? positionId = null, string? targetType = null, string? status = null)
    {
        var model = await _bannerAdminService.GetFormAsync(id);
        model.ReturnPage = Math.Max(1, page);
        model.ReturnKeyword = keyword;
        model.ReturnPositionId = positionId;
        model.ReturnTargetType = targetType;
        model.ReturnStatus = status;
        return View(model);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminBannerFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var reloadedModel = await _bannerAdminService.GetFormAsync(model.BannerId > 0 ? model.BannerId : null);
            MergeLookup(model, reloadedModel);
            return View("Form", model);
        }

        try
        {
            await _bannerAdminService.SaveAsync(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            var reloadedModel = await _bannerAdminService.GetFormAsync(model.BannerId > 0 ? model.BannerId : null);
            MergeLookup(model, reloadedModel);
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index), new
        {
            page = model.ReturnPage,
            keyword = model.ReturnKeyword,
            positionId = model.ReturnPositionId,
            targetType = model.ReturnTargetType,
            status = model.ReturnStatus
        });
    }

    [HttpGet("LookupProducts")]
    public async Task<IActionResult> LookupProducts(string? keyword)
    {
        var products = await _bannerAdminService.SearchProductsAsync(keyword);
        return Json(products);
    }

    [HttpDelete("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _bannerAdminService.DeleteAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Json(new { success = true, message = result.Message });
    }

    private static void MergeLookup(AdminBannerFormViewModel target, AdminBannerFormViewModel source)
    {
        target.Categories = source.Categories;
        target.Brands = source.Brands;
        target.Positions = source.Positions;
        target.InitialProducts = source.InitialProducts;
    }
}
