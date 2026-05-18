using Microsoft.AspNetCore.Mvc;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers;

[Area("Admin")]
[Route("admin/[controller]")]
public class SuppliersController : BaseAdminController
{
    private readonly ISupplierService _supplierService;
    private readonly IConfiguration _configuration;
    private const int FallbackPageSize = 20;

    public SuppliersController(ApplicationDbContext context, ISupplierService supplierService, IConfiguration configuration) : base(context)
    {
        _supplierService = supplierService;
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
    public async Task<IActionResult> Index(string? code, string? name, string? phoneNumber, string? status, int page = 1)
    {
        var model = await _supplierService.GetAdminSupplierIndexAsync(code, name, phoneNumber, status, page, GetDefaultAdminPageSize());
        return View(model);
    }

    [HttpGet("Form")]
    public async Task<IActionResult> Form(int? id, int page = 1, string? code = null, string? name = null, string? phoneNumber = null, string? status = null)
    {
        var model = await _supplierService.GetSupplierFormAsync(id);
        if (id.HasValue && model == null)
        {
            return NotFound();
        }

        var viewModel = model ?? new AdminSupplierFormViewModel();
        viewModel.ReturnPage = Math.Max(1, page);
        viewModel.ReturnCode = code;
        viewModel.ReturnName = name;
        viewModel.ReturnPhoneNumber = phoneNumber;
        viewModel.ReturnStatus = status;
        return View(viewModel);
    }

    [HttpPost("Save")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminSupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        try
        {
            await _supplierService.SaveSupplierAsync(model);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("Form", model);
        }

        return RedirectToAction(nameof(Index), new
        {
            page = model.ReturnPage,
            code = model.ReturnCode,
            name = model.ReturnName,
            phoneNumber = model.ReturnPhoneNumber,
            status = model.ReturnStatus
        });
    }

    [HttpDelete("Delete/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _supplierService.DeleteSupplierAsync(id);
        if (!result.Success)
        {
            return BadRequest(new { success = false, message = result.Message });
        }

        return Json(new { success = true, message = result.Message });
    }
}
