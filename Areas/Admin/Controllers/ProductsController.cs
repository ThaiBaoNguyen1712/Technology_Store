using Microsoft.AspNetCore.Mvc;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;
using Tech_Store.Services.Admin.ProductServices;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class ProductsController : BaseAdminController
    {
        private readonly ExcelService _excelService;
        private readonly IAdminProductService _adminProductService;

        public ProductsController(
            ApplicationDbContext context,
            IAdminProductService adminProductService,
            ExcelService excelService) : base(context)
        {
            _adminProductService = adminProductService;
            _excelService = excelService;
        }

        [Route("View/{id}")]
        public async Task<IActionResult> View(int id)
        {
            var data = await _adminProductService.GetDetailAsync(id);
            ViewBag.Total_Sell = data.TotalSell;
            ViewBag.Review_Count = data.ReviewCount;
            ViewBag.Order_Count = data.OrderCount;
            return View(data.Product);
        }

        [Route("ImportExcel")]
        public IActionResult ImportExcel()
        {
            return View();
        }

        [HttpPost("ImportExcel")]
        public async Task<IActionResult> ImportExcel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ViewBag.Error = "Vui lòng chọn file có đuôi .xlsx hoặc .csv";
                return View();
            }

            await Task.Yield();
            var result = _excelService.ImportExcel(file);
            ViewBag.SuccessCount = result.products.Count;
            ViewBag.ErrorCount = result.errors.Count;
            ViewBag.Errors = result.errors;
            return View();
        }

        [Route("{status?}")]
        [Route("Index/{status?}")]
        public async Task<IActionResult> Index(string? status, string? sku, string? name, int? categoryId, int? brandId, int? stockFrom, int? stockTo, int page = 1, int pageSize = 25)
        {
            var data = await _adminProductService.GetIndexDataAsync(new AdminProductFilterRequest
            {
                Status = status,
                Sku = sku,
                Name = name,
                CategoryId = categoryId,
                BrandId = brandId,
                StockFrom = stockFrom,
                StockTo = stockTo,
                Page = page,
                PageSize = pageSize
            });
            ViewBag.cate = data.Categories;
            ViewBag.brand = data.Brands;
            ViewBag.Filters = data.Filters;
            ViewBag.PageSize = data.PageSize;
            return View(data.Products);
        }

        [HttpPost]
        [Route("Filter")]
        public async Task<IActionResult> Filter(string sku, string name, string status, int? categoryId, int? brandId, int? stockFrom, int? stockTo)
        {
            var result = await _adminProductService.FilterAsync(new AdminProductFilterRequest
            {
                Sku = sku,
                Name = name,
                Status = status,
                CategoryId = categoryId,
                BrandId = brandId,
                StockFrom = stockFrom,
                StockTo = stockTo
            });

            return Json(result);
        }

        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            var lookup = await _adminProductService.GetLookupDataAsync();
            ViewBag.Categories = lookup.Categories;
            ViewBag.Brands = lookup.Brands;
            ViewBag.Attributes = lookup.Attributes;
            ViewBag.Specs = lookup.Specs;
            return View();
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductDTo productDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Invalid");
            }

            var result = await _adminProductService.CreateAsync(productDto);
            if (result.Success)
            {
                return RedirectToAction("Index");
            }

            ModelState.AddModelError("", result.Message);
            return View("Error");
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var data = await _adminProductService.GetEditDataAsync(id);
            if (data?.Product == null)
            {
                return NotFound("Không tìm thấy sản phẩm");
            }

            ViewBag.Attributes_checked = data.CheckedAttributeIds;
            ViewBag.Categories = data.Categories;
            ViewBag.Brands = data.Brands;
            ViewBag.Attributes = data.Attributes;
            ViewBag.Specs = data.Specs;
            ViewBag.ProductSpecValues = data.ProductSpecValues;
            return View(data.Product);
        }

        [HttpPost("Update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(ProductDTo productDto)
        {
            if (!ModelState.IsValid) return BadRequest("Invalid model state");

            var result = await _adminProductService.UpdateAsync(productDto);
            if (result.NotFound)
            {
                return NotFound();
            }

            if (!result.Success)
            {
                return StatusCode(500, result.Message);
            }

            return RedirectToAction("Index");
        }

        [HttpPost("ChangeVisible")]
        public async Task<JsonResult> ChangeVisible(int productId)
        {
            var result = await _adminProductService.ChangeVisibleAsync(productId);
            return Json(new { success = result.Success, message = result.Message, visible = result.Visible });
        }

        [HttpPost("Delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _adminProductService.DeleteAsync(id);
            if (!result.Success && result.NotFound)
            {
                return BadRequest("Không tìm thấy sản phẩm");
            }

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Json(new { success = true, message = result.Message });
        }

        [HttpPost("DeleteFileImage")]
        public async Task<IActionResult> DeleteFileImage([FromBody] DeleteFileViewModel model)
        {
            var result = await _adminProductService.DeleteMainImageAsync(model);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost("DeleteFileImageFromGalleries")]
        public async Task<IActionResult> DeleteFileImageFromGalleries([FromBody] DeleteFileViewModel model)
        {
            var result = await _adminProductService.DeleteGalleryImageAsync(model);
            if (result.NotFound)
            {
                return NotFound(result.Message);
            }

            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet("GenerateCode/{id}/{content?}/{codeType?}")]
        public async Task<IActionResult> GenerateCode(int id, string? content, string? codeType)
        {
            var data = await _adminProductService.GetCodeDataAsync(id, content, codeType, Request.Scheme);
            if (data?.Product == null)
            {
                return NotFound();
            }

            ViewBag.QRCodeImage = data.QRCodeImage;
            ViewBag.BarcodeImage = data.BarcodeImage;
            ViewBag.Content = data.Content;
            ViewBag.CodeType = data.CodeType;

            return View(data.Product);
        }

        [HttpGet]
        [Route("PrintCode")]
        public async Task<JsonResult> PrintCode(int id, string content, string codeType, int quantity)
        {
            var result = await _adminProductService.BuildPrintCodeAsync(id, content, codeType, quantity);
            return Json(new { success = result.Success, message = result.Message, html = result.Html });
        }
    }
}
