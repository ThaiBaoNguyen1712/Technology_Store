using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Models;
using X.PagedList.Extensions;
using X.PagedList;
using OfficeOpenXml;
using System.Linq;
using OfficeOpenXml.Style;
using System.Drawing;
using System.Text;
using System.Text.Json;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;
namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class StockManagerController : BaseAdminController
    {
        private readonly IConfiguration _configuration;
        private readonly IStockManagerService _stockManagerService;
        private const int FallbackPageSize = 20;

        public StockManagerController(ApplicationDbContext context, IConfiguration configuration, IStockManagerService stockManagerService) : base(context)
        {
            _configuration = configuration;
            _stockManagerService = stockManagerService;
        }

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            var resolvedPageSize = pageSize.GetValueOrDefault(FallbackPageSize);
            return resolvedPageSize > 0 ? resolvedPageSize : FallbackPageSize;
        }

        [HttpGet("")]
        [HttpGet("index")]
        [HttpGet("inventory", Name = "AdminStockInventory")]
        public IActionResult Index(string? sku, string? name, string? status, int? categoryId, int? brandId, int? stockFrom, int? stockTo, int page = 1)
        {
            var pageSize = GetDefaultAdminPageSize();

            var products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(sku))
            {
                products = products.Where(p => p.Sku != null && p.Sku.Contains(sku));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                products = products.Where(p => p.Name.Contains(name));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                products = products.Where(p => p.Status == status);
            }

            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                products = products.Where(p => p.BrandId == brandId.Value);
            }

            if (stockFrom.HasValue)
            {
                products = products.Where(p => p.Stock >= stockFrom.Value);
            }

            if (stockTo.HasValue)
            {
                products = products.Where(p => p.Stock <= stockTo.Value);
            }

            var totalItems = products.Count();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var items = products
                .OrderByDescending(x => x.ProductId)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminStockIndexItemViewModel
                {
                    ProductId = p.ProductId,
                    ImageUrl = p.Image,
                    Name = p.Name,
                    Sku = p.Sku,
                    BrandName = p.Brand != null ? p.Brand.Name : null,
                    CategoryName = p.Category != null ? p.Category.Name : null,
                    SellPrice = p.SellPrice,
                    Stock = p.Stock,
                    Status = p.Status
                })
                .ToList();

            var model = new AdminStockIndexViewModel
            {
                Items = items,
                Categories = _context.Categories.OrderBy(x => x.Name).ToList(),
                Brands = _context.Brands.OrderBy(x => x.Name).ToList(),
                Sku = sku,
                Name = name,
                Status = status,
                CategoryId = categoryId,
                BrandId = brandId,
                StockFrom = stockFrom,
                StockTo = stockTo,
                Page = currentPage,
                TotalPages = totalPages,
                TotalItems = totalItems,
                QueryString = BuildStockIndexQueryString(sku, name, status, categoryId, brandId, stockFrom, stockTo)
            };

            return View(model);
        }

        [HttpPost]
        [Route("Filter")]
        public IActionResult Filter(string sku, string name, string status, int? categoryId, int? brandId, int? stockFrom, int? stockTo)
        {
            return RedirectToAction(nameof(Index), new
            {
                sku,
                name,
                status,
                categoryId,
                brandId,
                stockFrom,
                stockTo
            });
        }
        [HttpGet("GetProduct/{id}")]
        public async Task<JsonResult> GetProduct(int id)
        {
            var product = await _context.Products.Include(p => p.VarientProducts).FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy" });
            }
            return Json(new {success=true,product});
        }

        [HttpGet("CreateTransaction")]
        public async Task<IActionResult> CreateTransaction(int[] selectedProductIds, string type = "Import", int returnPage = 1, string? returnSku = null, string? returnName = null, string? returnStatus = null, int? returnCategoryId = null, int? returnBrandId = null, int? returnStockFrom = null, int? returnStockTo = null)
        {
            var model = await _stockManagerService.GetBatchTransactionFormAsync(selectedProductIds?.Distinct().ToArray() ?? Array.Empty<int>());
            model.Type = string.Equals(type, "Export", StringComparison.OrdinalIgnoreCase) ? "Export" : "Import";
            model.ReturnPage = returnPage;
            model.ReturnSku = returnSku;
            model.ReturnName = returnName;
            model.ReturnStatus = returnStatus;
            model.ReturnCategoryId = returnCategoryId;
            model.ReturnBrandId = returnBrandId;
            model.ReturnStockFrom = returnStockFrom;
            model.ReturnStockTo = returnStockTo;

            return View("BatchImport", model);
        }

        [HttpGet("EditTransaction/{id:int}")]
        public async Task<IActionResult> EditTransaction(int id, int returnPage = 1, int? returnPageSize = null, string? returnSku = null, string? returnName = null, string? returnStatus = null, int? returnCategoryId = null, int? returnBrandId = null, int? returnStockFrom = null, int? returnStockTo = null, string? returnSource = null, string? returnHistoryFilterCode = null, string? returnHistoryFilterType = null, DateOnly? returnHistoryStartDate = null, DateOnly? returnHistoryEndDate = null)
        {
            var model = await _stockManagerService.GetEditTransactionFormAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            model.ReturnPage = returnPage;
            model.ReturnPageSize = returnPageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            model.ReturnSku = returnSku;
            model.ReturnName = returnName;
            model.ReturnStatus = returnStatus;
            model.ReturnCategoryId = returnCategoryId;
            model.ReturnBrandId = returnBrandId;
            model.ReturnStockFrom = returnStockFrom;
            model.ReturnStockTo = returnStockTo;
            model.ReturnSource = string.IsNullOrWhiteSpace(returnSource) ? "stock-index" : returnSource;
            model.ReturnHistoryFilterCode = returnHistoryFilterCode;
            model.ReturnHistoryFilterType = returnHistoryFilterType;
            model.ReturnHistoryStartDate = returnHistoryStartDate;
            model.ReturnHistoryEndDate = returnHistoryEndDate;
            ViewData["InventoryTransId"] = model.InventoryTransId.GetValueOrDefault(id);

            return View("EditTransactionForm", model);
        }

        [HttpPost("CreateTransaction")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTransaction([FromForm] AdminStockBatchImportFormViewModel model)
        {
            try
            {
                await _stockManagerService.CreateTransactionAsync(model, GetCurrentUserId());
            }
            catch (InvalidOperationException ex)
            {
                await _stockManagerService.PrepareBatchTransactionFormAsync(model);
                return await BuildBatchTransactionErrorResultAsync(model, ex.Message);
            }

            return BuildTransactionSuccessResult("Tạo phiếu kho thành công.", "/Admin/StockManager/Transactions");
        }

        [HttpPost("SaveTransaction")]
        [HttpPost("SaveTransaction/{inventoryTransId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTransaction([FromForm] AdminStockTransactionFormViewModel model, int? inventoryTransId = null)
        {
            var resolvedInventoryTransId = inventoryTransId.GetValueOrDefault(model.InventoryTransId.GetValueOrDefault());
            if (resolvedInventoryTransId <= 0)
            {
                return BadRequest("Route lưu phiếu kho không hợp lệ.");
            }

            return await UpdateTransaction(resolvedInventoryTransId, model);
        }

        [HttpPost("UpdateTransaction/{inventoryTransId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTransaction(int inventoryTransId, [FromForm] AdminStockTransactionFormViewModel model)
        {
            if (Request.HasFormContentType)
            {
                var postedNote = Request.Form["Note"].ToString();
                if (!string.IsNullOrWhiteSpace(postedNote) || string.IsNullOrWhiteSpace(model.Note))
                {
                    model.Note = postedNote;
                }
            }

            try
            {
                await _stockManagerService.UpdateTransactionAsync(inventoryTransId, model, GetCurrentUserId());
            }
            catch (InvalidOperationException ex)
            {
                model.InventoryTransId = inventoryTransId;
                await _stockManagerService.PrepareTransactionFormAsync(model);
                return await BuildTransactionFormErrorResultAsync("EditTransactionForm", model, ex.Message);
            }

            return BuildTransactionSuccessResult("Cập nhật phiếu kho thành công.", BuildTransactionReturnUrl(model));
        }

        [HttpPost("DeleteTransaction/{inventoryTransId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(int inventoryTransId)
        {
            try
            {
                await _stockManagerService.DeleteTransactionAsync(inventoryTransId);
            }
            catch (InvalidOperationException ex)
            {
                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message = ex.Message });
                }

                TempData["error"] = ex.Message;
                return Redirect("/Admin/StockManager/Transactions");
            }

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message = "Xóa phiếu kho thành công.",
                    redirectUrl = "/Admin/StockManager/Transactions"
                });
            }

            TempData["success"] = "Xóa phiếu kho thành công.";
            return Redirect("/Admin/StockManager/Transactions");
        }

        [HttpGet("BatchTransactionProducts")]
        public async Task<IActionResult> BatchTransactionProducts(int[] selectedProductIds)
        {
            var products = await _stockManagerService.GetBatchTransactionProductsAsync(selectedProductIds?.Distinct().ToArray() ?? Array.Empty<int>());
            return Json(new { success = true, products });
        }

        [HttpGet("ProductPicker")]
        public IActionResult ProductPicker(string? keyword, int page = 1, int pageSize = 8)
        {
            var resolvedPageSize = Math.Clamp(pageSize, 5, 20);

            var query = _context.Products
                .AsNoTracking()
                .Include(x => x.Brand)
                .Include(x => x.Category)
                .Where(x => x.Visible == true && x.Status != "discontinued")
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    x.Name.Contains(normalizedKeyword) ||
                    (x.Sku != null && x.Sku.Contains(normalizedKeyword)));
            }

            var totalItems = query.Count();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var items = query
                .OrderByDescending(x => x.ProductId)
                .Skip((currentPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .Select(x => new AdminStockProductPickerItemViewModel
                {
                    ProductId = x.ProductId,
                    Name = x.Name,
                    Sku = x.Sku ?? string.Empty,
                    ProductImageUrl = x.Image,
                    BrandName = x.Brand != null ? x.Brand.Name : null,
                    CategoryName = x.Category != null ? x.Category.Name : null,
                    Stock = x.Stock,
                    VariantCount = x.VarientProducts.Count
                })
                .ToList();

            var model = new AdminStockProductPickerViewModel
            {
                Items = items,
                Keyword = keyword,
                Page = currentPage,
                PageSize = resolvedPageSize,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return PartialView("_ProductPickerResults", model);
        }

        [HttpGet("transactions", Name = "AdminStockTransactions")]
        [HttpGet("history")]
        public async Task<IActionResult> History(DateOnly? startDate, DateOnly? endDate, string? filterType, string? filterCode, int page = 1, int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            if (resolvedPageSize <= 0)
            {
                resolvedPageSize = GetDefaultAdminPageSize();
            }
            var model = await _stockManagerService.GetHistoryAsync(startDate, endDate, filterType, filterCode, page, resolvedPageSize);
            return View(model);
        }


        [HttpGet("GetHistoryDetail/{id}")]
        public async Task<JsonResult> GetHistoryDetail(int id)
        {
            var history = await _stockManagerService.GetHistoryDetailAsync(id);
            if (history == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chi tiết lịch sử" });
            }

            return Json(new { success = true, history });
        }
        [HttpPost("FilterHistoryDetail")]
        public IActionResult FilterHistoryDetail(DateOnly? startDate, DateOnly? endDate, string? filterType, string? filterCode, int page = 1, int? pageSize = null)
        {
            return RedirectToAction(nameof(History), new
            {
                startDate,
                endDate,
                filterType,
                filterCode,
                page,
                pageSize
            });
        }

        [HttpGet("ExportSelection")]
        public IActionResult ExportSelection(string? keyword, int? categoryId, string? status, int? stockFrom, int? stockTo, int page = 1)
        {
            var pageSize = GetDefaultAdminPageSize();
            var query = BuildExportSelectionQuery(keyword, categoryId, status, stockFrom, stockTo);

            var totalItems = query.Count();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var items = query
                .OrderByDescending(x => x.VarientId)
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new AdminStockExportVariantItemViewModel
                {
                    VariantId = x.VarientId,
                    ImageUrl = x.Product != null ? x.Product.Image : null,
                    ProductName = x.Product != null ? x.Product.Name : "N/A",
                    Sku = x.Sku,
                    AttributeSummary = x.Attributes ?? "N/A",
                    SellPrice = x.Price,
                    Stock = x.Stock ?? 0,
                    CategoryName = x.Product != null && x.Product.Category != null ? x.Product.Category.Name : null
                })
                .ToList();

            var model = new AdminStockExportIndexViewModel
            {
                Items = items,
                Keyword = keyword,
                CategoryId = categoryId,
                Status = status,
                StockFrom = stockFrom,
                StockTo = stockTo,
                Categories = _context.Categories.OrderBy(x => x.Name).ToList(),
                Page = currentPage,
                TotalPages = totalPages,
                TotalItems = totalItems,
                QueryString = BuildExportSelectionQueryString(keyword, categoryId, status, stockFrom, stockTo)
            };

            return View(model);
        }

        [HttpPost("ExportSelection")]
        [ValidateAntiForgeryToken]
        public IActionResult ExportSelection(string[]? selectedVariantIds, string? keyword, int? categoryId, string? status, int? stockFrom, int? stockTo, string? exportMode = null)
        {
            var normalizedExportMode = string.Equals(exportMode, "filtered", StringComparison.OrdinalIgnoreCase)
                ? "filtered"
                : "selected";

            List<int> variantIds;
            if (normalizedExportMode == "filtered")
            {
                variantIds = BuildExportSelectionQuery(keyword, categoryId, status, stockFrom, stockTo)
                    .OrderByDescending(x => x.VarientId)
                    .Select(x => x.VarientId)
                    .ToList();
            }
            else
            {
                variantIds = (selectedVariantIds ?? Array.Empty<string>())
                    .Select(value => int.TryParse(value, out var parsedId) ? parsedId : 0)
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();
            }

            if (variantIds.Count == 0)
            {
                TempData["error"] = normalizedExportMode == "filtered"
                    ? "Không có biến thể nào phù hợp với bộ lọc hiện tại để xuất Excel."
                    : "Vui lòng chọn ít nhất một biến thể để xuất Excel.";
                return RedirectToAction(nameof(ExportSelection), new
                {
                    keyword,
                    categoryId,
                    status,
                    stockFrom,
                    stockTo
                });
            }

            return ExportVariantsToExcel(variantIds);
        }

        private static string BuildExportSelectionQueryString(string? keyword, int? categoryId, string? status, int? stockFrom, int? stockTo)
        {
            var values = new List<string>();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                values.Add($"keyword={Uri.EscapeDataString(keyword)}");
            }

            if (categoryId.HasValue)
            {
                values.Add($"categoryId={categoryId.Value}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                values.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (stockFrom.HasValue)
            {
                values.Add($"stockFrom={stockFrom.Value}");
            }

            if (stockTo.HasValue)
            {
                values.Add($"stockTo={stockTo.Value}");
            }

            return string.Join("&", values);
        }

        private IQueryable<VarientProduct> BuildExportSelectionQuery(string? keyword, int? categoryId, string? status, int? stockFrom, int? stockTo)
        {
            var query = _context.VarientProducts
                .AsNoTracking()
                .Include(x => x.Product)
                .ThenInclude(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    x.Sku.Contains(normalizedKeyword) ||
                    (x.Product != null && x.Product.Name.Contains(normalizedKeyword)) ||
                    (x.Attributes != null && x.Attributes.Contains(normalizedKeyword)));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(x => x.Product != null && x.Product.CategoryId == categoryId.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(x => x.Product != null && x.Product.Status == status);
            }

            if (stockFrom.HasValue)
            {
                query = query.Where(x => (x.Stock ?? 0) >= stockFrom.Value);
            }

            if (stockTo.HasValue)
            {
                query = query.Where(x => (x.Stock ?? 0) <= stockTo.Value);
            }

            return query;
        }

        private Task<IActionResult> BuildTransactionFormErrorResultAsync(string viewName, AdminStockTransactionFormViewModel model, string message)
        {
            if (IsAjaxRequest())
            {
                return Task.FromResult<IActionResult>(Json(new
                {
                    success = false,
                    message
                }));
            }

            TempData["error"] = message;
            return Task.FromResult<IActionResult>(View(viewName, model));
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private Task<IActionResult> BuildBatchTransactionErrorResultAsync(AdminStockBatchImportFormViewModel model, string message)
        {
            if (IsAjaxRequest())
            {
                return Task.FromResult<IActionResult>(Json(new
                {
                    success = false,
                    message
                }));
            }

            TempData["error"] = message;
            return Task.FromResult<IActionResult>(View("BatchImport", model));
        }

        private int GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userId, out var parsedUserId) || parsedUserId <= 0)
            {
                throw new InvalidOperationException("Không xác định được người tạo phiếu kho.");
            }

            return parsedUserId;
        }

        private IActionResult BuildTransactionSuccessResult(string message, string redirectUrl)
        {
            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = true,
                    message,
                    redirectUrl
                });
            }

            TempData["success"] = message;
            return Redirect(redirectUrl);
        }

        private static string BuildTransactionReturnUrl(AdminStockTransactionFormViewModel model)
        {
            return "/Admin/StockManager/Transactions";
        }

        private static string BuildStockIndexQueryString(string? sku, string? name, string? status, int? categoryId, int? brandId, int? stockFrom, int? stockTo)
        {
            var values = new List<string>();

            if (!string.IsNullOrWhiteSpace(sku))
            {
                values.Add($"sku={Uri.EscapeDataString(sku)}");
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                values.Add($"name={Uri.EscapeDataString(name)}");
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                values.Add($"status={Uri.EscapeDataString(status)}");
            }

            if (categoryId.HasValue)
            {
                values.Add($"categoryId={categoryId.Value}");
            }

            if (brandId.HasValue)
            {
                values.Add($"brandId={brandId.Value}");
            }

            if (stockFrom.HasValue)
            {
                values.Add($"stockFrom={stockFrom.Value}");
            }

            if (stockTo.HasValue)
            {
                values.Add($"stockTo={stockTo.Value}");
            }

            return string.Join("&", values);
        }

        private IActionResult ExportVariantsToExcel(IReadOnlyCollection<int> variantIds)
        {
            var variants = _context.VarientProducts
                .Include(x => x.Product)
                .ThenInclude(p => p.Category)
                .Where(x => variantIds.Contains(x.VarientId))
                .OrderByDescending(x => x.VarientId)
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Products");

                // Định dạng tiêu đề
                var titleRow = 1;
                var headerRow = 3;

                // Thêm tiêu đề chính
                worksheet.Cells[titleRow, 1].Value = "BÁO CÁO THÔNG TIN SẢN PHẨM";
                using (var range = worksheet.Cells[titleRow, 1, titleRow, 6])
                {
                    range.Merge = true;
                    range.Style.Font.Size = 16;
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 220, 220, 220));
                }

                // Thêm ngày xuất báo cáo
                worksheet.Cells[titleRow + 1, 1].Value = $"Ngày xuất báo cáo: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}";
                using (var range = worksheet.Cells[titleRow + 1, 1, titleRow + 1, 6])
                {
                    range.Merge = true;
                    range.Style.Font.Italic = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }

                // Định dạng header
                var headers = new string[] { "SKU", "TÊN SẢN PHẨM", "BIẾN THỂ", "DANH MỤC", "ĐƠN GIÁ", "SL TỒN" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[headerRow, i + 1].Value = headers[i];
                    using (var range = worksheet.Cells[headerRow, i + 1])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 184, 204, 228));
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }

                // Điền dữ liệu và định dạng
                var dataStartRow = headerRow + 1;
                for (int i = 0; i < variants.Count; i++)
                {
                    var currentRow = dataStartRow + i;
                    var variant = variants[i];

                    worksheet.Cells[currentRow, 1].Value = variant.Sku ?? "N/A";
                    worksheet.Cells[currentRow, 2].Value = variant.Product?.Name ?? "N/A";
                    worksheet.Cells[currentRow, 3].Value = variant.Attributes ?? "N/A";
                    worksheet.Cells[currentRow, 4].Value = variant.Product?.Category?.Name ?? "N/A";
                    worksheet.Cells[currentRow, 5].Value = variant.Price ?? 0;
                    worksheet.Cells[currentRow, 6].Value = variant.Stock ?? 0;

                    // Định dạng các ô dữ liệu
                    using (var range = worksheet.Cells[currentRow, 1, currentRow, 6])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                        // Thêm màu xen kẽ cho các dòng
                        if (i % 2 == 1)
                        {
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 242, 242, 242));
                        }
                    }

                    // Định dạng cột giá
                    worksheet.Cells[currentRow, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    // Định dạng cột số lượng
                    worksheet.Cells[currentRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Thêm footer với tổng kết
                var footerRow = dataStartRow + variants.Count + 1;
                worksheet.Cells[footerRow, 1].Value = "Tổng cộng:";
                worksheet.Cells[footerRow, 1, footerRow, 4].Merge = true;
                worksheet.Cells[footerRow, 1, footerRow, 4].Style.Font.Bold = true;
                worksheet.Cells[footerRow, 1, footerRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                // Tính tổng giá trị và số lượng
                worksheet.Cells[footerRow, 5].Formula = $"SUM(E{dataStartRow}:E{footerRow - 1})";
                worksheet.Cells[footerRow, 6].Formula = $"SUM(F{dataStartRow}:F{footerRow - 1})";
                worksheet.Cells[footerRow, 5].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[footerRow, 5, footerRow, 6].Style.Font.Bold = true;
                worksheet.Cells[footerRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[footerRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Thêm ghi chú cuối trang
                var noteRow = footerRow + 2;
                worksheet.Cells[noteRow, 1].Value = "Ghi chú:";
                worksheet.Cells[noteRow, 2, noteRow, 6].Value = "Báo cáo được tạo tự động từ hệ thống";
                worksheet.Cells[noteRow, 1].Style.Font.Bold = true;
                worksheet.Cells[noteRow, 2, noteRow, 6].Style.Font.Italic = true;

                // Tự động điều chỉnh độ rộng cột
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Đặt độ rộng tối thiểu và tối đa cho các cột
                for (int i = 1; i <= 6; i++)
                {
                    double width = worksheet.Column(i).Width;
                    if (width < 10) worksheet.Column(i).Width = 10;
                    else if (width > 50) worksheet.Column(i).Width = 50;
                }

                // Thêm bảo vệ worksheet (tùy chọn)
                worksheet.Protection.IsProtected = true;
                worksheet.Protection.AllowSelectLockedCells = true;

                // Lưu file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Products_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                );
            }
        }


    }
}
