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
namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class StockManagerController : BaseAdminController
    {
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;
        private static readonly JsonSerializerOptions BatchTransactionJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public StockManagerController(ApplicationDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
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

        [HttpGet("CreateTransaction/{productId:int}")]
        public async Task<IActionResult> CreateTransaction(int productId, int returnPage = 1, int? returnPageSize = null, string? returnSku = null, string? returnName = null, string? returnStatus = null, int? returnCategoryId = null, int? returnBrandId = null, int? returnStockFrom = null, int? returnStockTo = null)
        {
            var model = await BuildTransactionFormModelAsync(productId, null);
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

            return View("CreateTransactionForm", model);
        }

        [HttpGet("BatchTransaction")]
        public async Task<IActionResult> BatchTransaction(int[] selectedProductIds, string type = "Import", int returnPage = 1, string? returnSku = null, string? returnName = null, string? returnStatus = null, int? returnCategoryId = null, int? returnBrandId = null, int? returnStockFrom = null, int? returnStockTo = null)
        {
            var model = selectedProductIds != null && selectedProductIds.Length > 0
                ? await BuildBatchImportFormModelAsync(selectedProductIds.Distinct().ToArray())
                : new AdminStockBatchImportFormViewModel
                {
                    Suppliers = await GetSupplierOptionsAsync()
                };

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
            var model = await BuildTransactionFormModelAsync(null, id);
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

        [HttpPost("SaveCreateTransaction")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveCreateTransaction(AdminStockTransactionFormViewModel model)
        {
            if (Request.HasFormContentType &&
                int.TryParse(Request.Form["InventoryTransId"].ToString(), out var postedInventoryTransId) &&
                postedInventoryTransId > 0)
            {
                model.InventoryTransId = postedInventoryTransId;
                return await SaveTransaction(model, isUpdate: true, postedInventoryTransId);
            }

            model.InventoryTransId = null;
            return await SaveTransaction(model, isUpdate: false);
        }

        [HttpPost("SaveTransaction")]
        [HttpPost("SaveTransaction/{inventoryTransId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTransactionLegacy(AdminStockTransactionFormViewModel model, int? inventoryTransId = null)
        {
            var resolvedInventoryTransId = inventoryTransId.GetValueOrDefault();
            if (resolvedInventoryTransId <= 0 &&
                Request.HasFormContentType &&
                int.TryParse(Request.Form["InventoryTransId"].ToString(), out var postedInventoryTransId) &&
                postedInventoryTransId > 0)
            {
                resolvedInventoryTransId = postedInventoryTransId;
            }

            if (resolvedInventoryTransId > 0)
            {
                model.InventoryTransId = resolvedInventoryTransId;
                return await SaveTransaction(model, isUpdate: true, resolvedInventoryTransId);
            }

            model.InventoryTransId = null;
            return await SaveTransaction(model, isUpdate: false);
        }

        [HttpPost("UpdateTransaction/{inventoryTransId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTransaction(int inventoryTransId, AdminStockTransactionFormViewModel model)
        {
            if (inventoryTransId <= 0 &&
                Request.HasFormContentType &&
                int.TryParse(Request.Form["InventoryTransId"].ToString(), out var postedInventoryTransId) &&
                postedInventoryTransId > 0)
            {
                inventoryTransId = postedInventoryTransId;
            }

            model.InventoryTransId = inventoryTransId;
            return await SaveTransaction(model, isUpdate: true, inventoryTransId);
        }

        private async Task<IActionResult> SaveTransaction(AdminStockTransactionFormViewModel model, bool isUpdate, int? inventoryTransId = null)
        {
            ApplyTransactionFormRequestFallbacks(model, inventoryTransId);
            if (isUpdate)
            {
                model.InventoryTransId = inventoryTransId.GetValueOrDefault(model.InventoryTransId.GetValueOrDefault());
                if (!model.InventoryTransId.HasValue || model.InventoryTransId.Value <= 0)
                {
                    return await BuildTransactionFormErrorResultAsync(model, "Không xác định được phiếu kho cần cập nhật.");
                }
            }
            else
            {
                model.InventoryTransId = null;
            }

            model.Products = ResolveTransactionFormProducts(model);

            var validProducts = model.Products
                .Select(product => new
                {
                    Product = product,
                    Variants = product.Variants.Where(variant => variant.Quantity > 0).ToList()
                })
                .Where(x => x.Variants.Count > 0)
                .ToList();

            var isImport = string.Equals(model.Type, "Import", StringComparison.OrdinalIgnoreCase);

            if (validProducts.Count == 0)
            {
                return await BuildTransactionFormErrorResultAsync(model, "Cần nhập ít nhất một biến thể có số lượng lớn hơn 0.");
            }

            if (isImport && !model.SupplierId.HasValue)
            {
                return await BuildTransactionFormErrorResultAsync(model, "Phiếu nhập kho cần gắn với một nhà cung cấp.");
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userId, out var parsedUserId) || parsedUserId <= 0)
                {
                    throw new InvalidOperationException("Không xác định được người tạo phiếu kho.");
                }

                var createdAt = DateTime.Now;
                var transactionUserId = parsedUserId;
                var existingTransactions = new List<InventoryTransactions>();
                if (isUpdate)
                {
                    var seedTransaction = await _context.InventoryTransactions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(x => x.InventoryTransId == model.InventoryTransId!.Value);

                    if (seedTransaction == null)
                    {
                        return NotFound();
                    }

                    createdAt = seedTransaction.CreatedAt ?? DateTime.Now;
                    transactionUserId = seedTransaction.UserId;

                    existingTransactions = await GetInventoryTransactionGroupQuery(seedTransaction)
                        .Include(x => x.Product)
                        .ThenInclude(x => x.VarientProducts)
                        .Include(x => x.InventoryTransactionsDetail)
                        .ToListAsync();

                    foreach (var existingTransaction in existingTransactions)
                    {
                        await RevertInventoryTransactionAsync(
                            existingTransaction.Product,
                            existingTransaction.Type,
                            existingTransaction.InventoryTransactionsDetail);
                    }

                    _context.InventoryTransactionsDetail.RemoveRange(existingTransactions.SelectMany(x => x.InventoryTransactionsDetail));
                    await _context.SaveChangesAsync();
                }

                var validProductIds = validProducts.Select(x => x.Product.ProductId).Distinct().ToArray();
                var products = await _context.Products
                    .Include(x => x.VarientProducts)
                    .Where(x => validProductIds.Contains(x.ProductId))
                    .ToDictionaryAsync(x => x.ProductId);

                var existingByProductId = existingTransactions
                    .GroupBy(x => x.ProductId)
                    .ToDictionary(x => x.Key, x => x.OrderByDescending(t => t.InventoryTransId).First());
                var obsoleteTransactions = existingTransactions
                    .Where(x => !validProductIds.Contains(x.ProductId))
                    .ToList();
                if (obsoleteTransactions.Count > 0)
                {
                    _context.InventoryTransactions.RemoveRange(obsoleteTransactions);
                }

                foreach (var productInput in validProducts)
                {
                    if (!products.TryGetValue(productInput.Product.ProductId, out var product))
                    {
                        throw new InvalidOperationException($"Không tìm thấy sản phẩm #{productInput.Product.ProductId}.");
                    }

                    if (!existingByProductId.TryGetValue(product.ProductId, out var inventoryTransaction))
                    {
                        inventoryTransaction = new InventoryTransactions
                        {
                            ProductId = product.ProductId,
                            Type = isImport ? "Import" : "Export",
                            UserId = transactionUserId,
                            SupplierId = isImport ? model.SupplierId : null,
                            Note = model.Note,
                            CreatedAt = createdAt,
                            UpdatedAt = DateTime.Now
                        };
                        await _context.InventoryTransactions.AddAsync(inventoryTransaction);
                        await _context.SaveChangesAsync();
                    }

                    var detailEntities = new List<InventoryTransactionsDetail>();
                    foreach (var variantInput in productInput.Variants)
                    {
                        var variant = product.VarientProducts.FirstOrDefault(x => x.VarientId == variantInput.VariantId);
                        if (variant == null)
                        {
                            throw new InvalidOperationException($"Không tìm thấy biến thể {variantInput.VariantId} của sản phẩm {product.Name}.");
                        }

                        if (isImport)
                        {
                            product.Stock += variantInput.Quantity;
                            variant.Stock = (variant.Stock ?? 0) + variantInput.Quantity;
                        }
                        else
                        {
                            if ((variant.Stock ?? 0) < variantInput.Quantity || product.Stock < variantInput.Quantity)
                            {
                                throw new InvalidOperationException($"Tồn kho của biến thể {variant.Sku} trong sản phẩm {product.Name} không đủ để xuất.");
                            }

                            product.Stock -= variantInput.Quantity;
                            variant.Stock = (variant.Stock ?? 0) - variantInput.Quantity;
                        }

                        detailEntities.Add(new InventoryTransactionsDetail
                        {
                            InventoryTransId = inventoryTransaction.InventoryTransId,
                            VarientId = variant.VarientId,
                            Quantity = variantInput.Quantity,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }

                    inventoryTransaction.Type = isImport ? "Import" : "Export";
                    inventoryTransaction.SupplierId = isImport ? model.SupplierId : null;
                    inventoryTransaction.Note = model.Note;
                    inventoryTransaction.UpdatedAt = DateTime.Now;
                    inventoryTransaction.ProductId = product.ProductId;
                    product.Status = ResolveStockStatus(product.Stock, product.Status);
                    await _context.InventoryTransactionsDetail.AddRangeAsync(detailEntities);
                }

                await _context.SaveChangesAsync();
                EnsureNonNegativeStock(products.Values);
                await dbTransaction.CommitAsync();

                var successMessage = isUpdate
                    ? "Cập nhật phiếu kho thành công."
                    : "Tạo phiếu kho thành công.";
                var redirectUrl = BuildTransactionReturnUrl(model);

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = true,
                        message = successMessage,
                        redirectUrl
                    });
                }

                TempData["success"] = successMessage;
                return Redirect(redirectUrl);
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return await BuildTransactionFormErrorResultAsync(model, GetRootExceptionMessage(ex));
            }
        }

        [HttpPost("DeleteTransaction/{inventoryTransId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTransaction(int inventoryTransId)
        {
            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var seedTransaction = await _context.InventoryTransactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InventoryTransId == inventoryTransId);

                if (seedTransaction == null)
                {
                    return IsAjaxRequest()
                        ? Json(new { success = false, message = "Không tìm thấy phiếu kho cần xóa." })
                        : NotFound();
                }

                var existingTransactions = await GetInventoryTransactionGroupQuery(seedTransaction)
                    .Include(x => x.Product)
                    .ThenInclude(x => x.VarientProducts)
                    .Include(x => x.InventoryTransactionsDetail)
                    .ToListAsync();

                if (existingTransactions.Count == 0)
                {
                    return IsAjaxRequest()
                        ? Json(new { success = false, message = "Không tìm thấy nhóm phiếu kho cần xóa." })
                        : NotFound();
                }

                foreach (var existingTransaction in existingTransactions)
                {
                    await RevertInventoryTransactionAsync(
                        existingTransaction.Product,
                        existingTransaction.Type,
                        existingTransaction.InventoryTransactionsDetail);
                }

                EnsureNonNegativeStock(existingTransactions.Select(x => x.Product).DistinctBy(x => x.ProductId));
                foreach (var product in existingTransactions.Select(x => x.Product).DistinctBy(x => x.ProductId))
                {
                    product.Status = ResolveStockStatus(product.Stock, product.Status);
                }

                _context.InventoryTransactionsDetail.RemoveRange(existingTransactions.SelectMany(x => x.InventoryTransactionsDetail));
                await _context.SaveChangesAsync();

                _context.InventoryTransactions.RemoveRange(existingTransactions);
                await _context.SaveChangesAsync();

                await dbTransaction.CommitAsync();

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
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                var message = GetRootExceptionMessage(ex);

                if (IsAjaxRequest())
                {
                    return Json(new { success = false, message });
                }

                TempData["error"] = message;
                return Redirect("/Admin/StockManager/Transactions");
            }
        }

        [HttpPost("SaveBatchTransaction")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBatchTransaction(AdminStockBatchImportFormViewModel model)
        {
            model.Products = ResolveBatchTransactionProducts(model);

            var validProducts = model.Products
                .Select(product => new
                {
                    Product = product,
                    Variants = product.Variants.Where(variant => variant.Quantity > 0).ToList()
                })
                .Where(x => x.Variants.Count > 0)
                .ToList();

            var isImport = string.Equals(model.Type, "Import", StringComparison.OrdinalIgnoreCase);

            if (isImport && !model.SupplierId.HasValue)
            {
                return await BuildBatchTransactionErrorResultAsync(model, "Phiếu nhập kho cần gắn với một nhà cung cấp.");
            }

            if (validProducts.Count == 0)
            {
                return await BuildBatchTransactionErrorResultAsync(model, "Cần nhập ít nhất một biến thể có số lượng lớn hơn 0.");
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userId, out var parsedUserId) || parsedUserId <= 0)
                {
                    throw new InvalidOperationException("Không xác định được người tạo phiếu kho.");
                }

                var batchCreatedAt = DateTime.Now;
                var batchNote = model.Note?.Trim();
                var selectedProductIds = validProducts.Select(x => x.Product.ProductId).Distinct().ToArray();
                var products = await _context.Products
                    .Include(x => x.VarientProducts)
                    .Where(x => selectedProductIds.Contains(x.ProductId))
                    .ToDictionaryAsync(x => x.ProductId);

                foreach (var productInput in validProducts)
                {
                    if (!products.TryGetValue(productInput.Product.ProductId, out var product))
                    {
                        throw new InvalidOperationException($"Không tìm thấy sản phẩm #{productInput.Product.ProductId}.");
                    }

                    var inventoryTransaction = new InventoryTransactions
                    {
                        ProductId = product.ProductId,
                        Type = isImport ? "Import" : "Export",
                        UserId = parsedUserId,
                        SupplierId = isImport ? model.SupplierId : null,
                        Note = batchNote,
                        CreatedAt = batchCreatedAt,
                        UpdatedAt = batchCreatedAt
                    };

                    await _context.InventoryTransactions.AddAsync(inventoryTransaction);
                    await _context.SaveChangesAsync();

                    var detailEntities = new List<InventoryTransactionsDetail>();

                    foreach (var variantInput in productInput.Variants)
                    {
                        var variant = product.VarientProducts.FirstOrDefault(x => x.VarientId == variantInput.VariantId);
                        if (variant == null)
                        {
                            throw new InvalidOperationException($"Không tìm thấy biến thể {variantInput.VariantId} của sản phẩm {product.Name}.");
                        }

                        if (isImport)
                        {
                            product.Stock += variantInput.Quantity;
                            variant.Stock = (variant.Stock ?? 0) + variantInput.Quantity;
                        }
                        else
                        {
                            if ((variant.Stock ?? 0) < variantInput.Quantity || product.Stock < variantInput.Quantity)
                            {
                                throw new InvalidOperationException($"Tồn kho của biến thể {variant.Sku} trong sản phẩm {product.Name} không đủ để xuất.");
                            }

                            product.Stock -= variantInput.Quantity;
                            variant.Stock = (variant.Stock ?? 0) - variantInput.Quantity;
                        }

                        detailEntities.Add(new InventoryTransactionsDetail
                        {
                            InventoryTransId = inventoryTransaction.InventoryTransId,
                            VarientId = variant.VarientId,
                            Quantity = variantInput.Quantity,
                            CreatedAt = DateTime.Now,
                            UpdatedAt = DateTime.Now
                        });
                    }

                    product.Status = ResolveStockStatus(product.Stock, product.Status);
                    await _context.InventoryTransactionsDetail.AddRangeAsync(detailEntities);
                    await _context.SaveChangesAsync();
                }

                await dbTransaction.CommitAsync();
                TempData["success"] = isImport
                    ? $"Đã tạo phiếu nhập cho {validProducts.Count} sản phẩm."
                    : $"Đã tạo phiếu xuất cho {validProducts.Count} sản phẩm.";

                if (IsAjaxRequest())
                {
                    return Json(new
                    {
                        success = true,
                        redirectUrl = "/Admin/StockManager/Transactions"
                    });
                }

                return Redirect("/Admin/StockManager/Transactions");
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                return await BuildBatchTransactionErrorResultAsync(model, ex.Message);
            }
        }

        [HttpGet("BatchTransactionProducts")]
        public async Task<IActionResult> BatchTransactionProducts(int[] selectedProductIds)
        {
            if (selectedProductIds == null || selectedProductIds.Length == 0)
            {
                return Json(new { success = true, products = Array.Empty<AdminStockBatchImportProductViewModel>() });
            }

            var model = await BuildBatchImportFormModelAsync(selectedProductIds.Distinct().ToArray());
            return Json(new { success = true, products = model.Products });
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
        public IActionResult History(DateOnly? startDate, DateOnly? endDate, string? filterType, string? filterCode, int page = 1, int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            if (resolvedPageSize <= 0)
            {
                resolvedPageSize = GetDefaultAdminPageSize();
            }

            var query = _context.InventoryTransactions
                .Include(p => p.Product)
                .Include(p => p.InventoryTransactionsDetail)
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .ThenInclude(p => p.Roles)
                .AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(ph => ph.CreatedAt >= startDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            if (endDate.HasValue)
            {
                query = query.Where(ph => ph.CreatedAt <= endDate.Value.ToDateTime(TimeOnly.MaxValue));
            }

            if (!string.IsNullOrWhiteSpace(filterType))
            {
                query = query.Where(ph => ph.Type.ToLower() == filterType.ToLower());
            }

            if (!string.IsNullOrWhiteSpace(filterCode))
            {
                query = query.Where(ph =>
                    ph.Product.Sku.Contains(filterCode) ||
                    ph.Product.Name.Contains(filterCode) ||
                    ph.InventoryTransId.ToString().Contains(filterCode));
            }

            var rawHistory = query
                .OrderByDescending(ph => ph.InventoryTransId)
                .Select(ph => new InventoryTransactionsVM
                {
                    Id = ph.InventoryTransId,
                    UserId = ph.UserId,
                    SupplierId = ph.SupplierId,
                    Product = ph.Product,
                    InventoryTransId = ph.InventoryTransId,
                    Type = ph.Type,
                    Note = ph.Note ?? string.Empty,
                    CreatedAt = ph.CreatedAt ?? DateTime.MinValue,
                    ProductCount = 1,
                    TotalQuantity = ph.InventoryTransactionsDetail.Sum(d => d.Quantity),
                    UserName = ph.User.LastName + " " + ph.User.FirstName,
                    UserRole = ph.User.Roles.FirstOrDefault().RoleName ?? "Không có vai trò",
                    SupplierName = ph.Supplier != null ? ph.Supplier.Name : null,
                    SupplierCode = ph.Supplier != null ? ph.Supplier.Code : null,
                    Products = new List<InventoryTransactionProductSummaryViewModel>
                    {
                        new InventoryTransactionProductSummaryViewModel
                        {
                            InventoryTransId = ph.InventoryTransId,
                            ProductId = ph.ProductId,
                            ProductName = ph.Product.Name,
                            ProductSku = ph.Product.Sku ?? string.Empty,
                            ProductImageUrl = ph.Product.Image,
                            TotalQuantity = ph.InventoryTransactionsDetail.Sum(d => d.Quantity)
                        }
                    },
                    InventoryTransactionDetail = ph.InventoryTransactionsDetail
                        .Select(d => new InventorTransactionDetailViewModel
                        {
                            InventoryTransId = d.InventoryTransId,
                            VarientId = d.VarientId,
                            Quantity = d.Quantity
                        })
                        .ToList()
                })
                .ToList();

            var groupedHistory = rawHistory
                .GroupBy(BuildInventoryTransactionGroupKey)
                .Select(group =>
                {
                    var orderedGroup = group
                        .OrderBy(x => x.InventoryTransId)
                        .ToList();
                    var primary = orderedGroup[0];

                    return new InventoryTransactionsVM
                    {
                        Id = primary.Id,
                        InventoryTransId = primary.InventoryTransId,
                        UserId = primary.UserId,
                        SupplierId = primary.SupplierId,
                        Product = primary.Product,
                        Type = primary.Type,
                        Note = primary.Note,
                        CreatedAt = primary.CreatedAt,
                        ProductCount = orderedGroup.Count,
                        TotalQuantity = orderedGroup.Sum(x => x.TotalQuantity),
                        UserName = primary.UserName,
                        UserRole = primary.UserRole,
                        SupplierName = primary.SupplierName,
                        SupplierCode = primary.SupplierCode,
                        Products = orderedGroup
                            .SelectMany(x => x.Products)
                            .OrderBy(x => x.ProductName)
                            .ToList(),
                        InventoryTransactionDetail = orderedGroup
                            .SelectMany(x => x.InventoryTransactionDetail)
                            .ToList()
                    };
                })
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.InventoryTransId)
                .ToList();

            var importCount = groupedHistory.Count(ph => ph.Type == "Import");
            var exportCount = groupedHistory.Count(ph => ph.Type == "Export");
            var totalItems = groupedHistory.Count;
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);
            var history = groupedHistory
                .Skip((currentPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .ToList();

            var model = new AdminStockHistoryIndexViewModel
            {
                Items = history,
                FilterCode = filterCode,
                FilterType = filterType,
                StartDate = startDate,
                EndDate = endDate,
                Page = currentPage,
                PageSize = resolvedPageSize,
                TotalPages = totalPages,
                TotalItems = totalItems,
                ImportCount = importCount,
                ExportCount = exportCount,
                QueryString = BuildHistoryQueryString(startDate, endDate, filterType, filterCode, resolvedPageSize)
            };

            return View(model);
        }


        [HttpGet("GetHistoryDetail/{id}")]
        public async Task<JsonResult> GetHistoryDetail(int id)
        {
            var selectedTransaction = await _context.InventoryTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(ph => ph.InventoryTransId == id);

            if (selectedTransaction == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chi tiết lịch sử" });
            }

            var histories = await _context.InventoryTransactions
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .ThenInclude(p => p.Roles)
                .Include(p => p.InventoryTransactionsDetail).ThenInclude(p => p.Varient)
                .Where(ph =>
                    ph.CreatedAt == selectedTransaction.CreatedAt &&
                    ph.UserId == selectedTransaction.UserId &&
                    ph.Type == selectedTransaction.Type &&
                    ph.SupplierId == selectedTransaction.SupplierId &&
                    (ph.Note ?? string.Empty) == (selectedTransaction.Note ?? string.Empty))
                .Select(ph => new InventoryTransactionsVM
                {
                    Id = ph.InventoryTransId,
                    InventoryTransId = ph.InventoryTransId,
                    UserId = ph.UserId,
                    SupplierId = ph.SupplierId,
                    Product = ph.Product,
                    Type = ph.Type,
                    Note = ph.Note ?? string.Empty,
                    CreatedAt = ph.CreatedAt ?? DateTime.MinValue,
                    ProductCount = 1,
                    TotalQuantity = ph.InventoryTransactionsDetail.Sum(d => d.Quantity),
                    InventoryTransactionDetail = ph.InventoryTransactionsDetail.Select(d => new InventorTransactionDetailViewModel
                    {
                        InventoryTransId = d.InventoryTransId,
                        VarientId = d.VarientId,
                        VarientSku = d.Varient.Sku,
                        Quantity = d.Quantity,
                        VarientName = d.Varient.Attributes ?? "N/A"
                    }).ToList(),
                    Products = new List<InventoryTransactionProductSummaryViewModel>
                    {
                        new InventoryTransactionProductSummaryViewModel
                        {
                            InventoryTransId = ph.InventoryTransId,
                            ProductId = ph.ProductId,
                            ProductName = ph.Product.Name,
                            ProductSku = ph.Product.Sku ?? string.Empty,
                            ProductImageUrl = ph.Product.Image,
                            TotalQuantity = ph.InventoryTransactionsDetail.Sum(d => d.Quantity),
                            Details = ph.InventoryTransactionsDetail.Select(d => new InventorTransactionDetailViewModel
                            {
                                InventoryTransId = d.InventoryTransId,
                                VarientId = d.VarientId,
                                VarientSku = d.Varient.Sku,
                                Quantity = d.Quantity,
                                VarientName = d.Varient.Attributes ?? "N/A"
                            }).ToList()
                        }
                    },
                    UserName = ph.User.LastName + " " + ph.User.FirstName,
                    UserRole = ph.User.Roles.FirstOrDefault().RoleName ?? "Không có vai trò",
                    SupplierName = ph.Supplier != null ? ph.Supplier.Name : null,
                    SupplierCode = ph.Supplier != null ? ph.Supplier.Code : null
                })
                .OrderByDescending(ph => ph.InventoryTransId)
                .ToListAsync();

            var history = histories
                .GroupBy(BuildInventoryTransactionGroupKey)
                .Select(group =>
                {
                    var primary = group.OrderBy(x => x.InventoryTransId).First();
                    return new InventoryTransactionsVM
                    {
                        Id = primary.Id,
                        InventoryTransId = primary.InventoryTransId,
                        UserId = primary.UserId,
                        SupplierId = primary.SupplierId,
                        Product = primary.Product,
                        Type = primary.Type,
                        Note = primary.Note,
                        CreatedAt = primary.CreatedAt,
                        ProductCount = group.Count(),
                        TotalQuantity = group.Sum(x => x.TotalQuantity),
                        UserName = primary.UserName,
                        UserRole = primary.UserRole,
                        SupplierName = primary.SupplierName,
                        SupplierCode = primary.SupplierCode,
                        Products = group.SelectMany(x => x.Products).OrderBy(x => x.ProductName).ToList(),
                        InventoryTransactionDetail = group.SelectMany(x => x.InventoryTransactionDetail).ToList()
                    };
                })
                .FirstOrDefault();

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

        private static string BuildHistoryQueryString(DateOnly? startDate, DateOnly? endDate, string? filterType, string? filterCode, int pageSize)
        {
            var values = new List<string>();

            if (startDate.HasValue)
            {
                values.Add($"startDate={startDate.Value:yyyy-MM-dd}");
            }

            if (endDate.HasValue)
            {
                values.Add($"endDate={endDate.Value:yyyy-MM-dd}");
            }

            if (!string.IsNullOrWhiteSpace(filterType))
            {
                values.Add($"filterType={Uri.EscapeDataString(filterType)}");
            }

            if (!string.IsNullOrWhiteSpace(filterCode))
            {
                values.Add($"filterCode={Uri.EscapeDataString(filterCode)}");
            }

            values.Add($"pageSize={pageSize}");

            return string.Join("&", values);
        }

        [HttpGet("ExportSelection")]
        public IActionResult ExportSelection(string? keyword, int? categoryId, string? status, int? stockFrom, int? stockTo, int page = 1)
        {
            var pageSize = GetDefaultAdminPageSize();

            var query = _context.VarientProducts
                .Include(x => x.Product)
                .ThenInclude(x => x.Category)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(x =>
                    x.Sku.Contains(keyword) ||
                    (x.Product != null && x.Product.Name.Contains(keyword)) ||
                    (x.Attributes != null && x.Attributes.Contains(keyword)));
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
        public IActionResult ExportSelection(string[] selectedVariantIds, string? keyword, int? categoryId, string? status, int? stockFrom, int? stockTo)
        {
            if (selectedVariantIds == null || selectedVariantIds.Length == 0)
            {
                TempData["error"] = "Vui lòng chọn ít nhất một biến thể để xuất Excel.";
                return RedirectToAction(nameof(ExportSelection), new
                {
                    keyword,
                    categoryId,
                    status,
                    stockFrom,
                    stockTo
                });
            }

            return ExportToExcel(selectedVariantIds);
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

        private IQueryable<InventoryTransactions> GetInventoryTransactionGroupQuery(InventoryTransactions seedTransaction)
        {
            var seedNote = seedTransaction.Note ?? string.Empty;

            return _context.InventoryTransactions.Where(ph =>
                ph.CreatedAt == seedTransaction.CreatedAt &&
                ph.UserId == seedTransaction.UserId &&
                ph.Type == seedTransaction.Type &&
                ph.SupplierId == seedTransaction.SupplierId &&
                (ph.Note ?? string.Empty) == seedNote);
        }

        private static AdminStockBatchImportProductViewModel BuildTransactionProductViewModel(
            Product product,
            IEnumerable<InventoryTransactionsDetail> details)
        {
            var detailLookup = details
                .GroupBy(x => x.VarientId)
                .ToDictionary(x => x.Key, x => x.Sum(detail => detail.Quantity));

            return new AdminStockBatchImportProductViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku ?? string.Empty,
                ProductImageUrl = product.Image,
                Variants = product.VarientProducts
                    .OrderBy(x => x.Sku)
                    .Select(x => new AdminStockTransactionVariantViewModel
                    {
                        VariantId = x.VarientId,
                        Sku = x.Sku,
                        AttributeSummary = x.Attributes ?? "N/A",
                        Price = x.Price,
                        CurrentStock = x.Stock ?? 0,
                        PreviousQuantity = detailLookup.TryGetValue(x.VarientId, out var previousQuantity) ? previousQuantity : 0,
                        Quantity = detailLookup.TryGetValue(x.VarientId, out var quantity) ? quantity : 0
                    })
                    .ToList()
            };
        }

        private static List<AdminStockBatchImportProductViewModel> ResolveTransactionFormProducts(AdminStockTransactionFormViewModel model)
        {
            var jsonProducts = new List<AdminStockBatchImportProductViewModel>();
            if (!string.IsNullOrWhiteSpace(model.ProductPayloadJson))
            {
                try
                {
                    jsonProducts = ParseBatchTransactionProducts(model.ProductPayloadJson);
                }
                catch (JsonException)
                {
                    jsonProducts = new List<AdminStockBatchImportProductViewModel>();
                }
            }

            if (HasPositiveQuantity(jsonProducts))
            {
                return jsonProducts;
            }

            if (HasPositiveQuantity(model.Products))
            {
                return model.Products;
            }

            if (jsonProducts.Count > 0)
            {
                return jsonProducts;
            }

            if (model.Products.Count > 0)
            {
                return model.Products;
            }

            if (model.Variants.Count == 0)
            {
                return new List<AdminStockBatchImportProductViewModel>();
            }

            return new List<AdminStockBatchImportProductViewModel>
            {
                new()
                {
                    ProductId = model.ProductId,
                    ProductName = model.ProductName,
                    ProductSku = model.ProductSku,
                    ProductImageUrl = model.ProductImageUrl,
                    Variants = model.Variants
                }
            };
        }

        private void ApplyTransactionFormRequestFallbacks(AdminStockTransactionFormViewModel model, int? routeInventoryTransId = null)
        {
            if (!Request.HasFormContentType)
            {
                if (!model.InventoryTransId.HasValue && routeInventoryTransId.HasValue && routeInventoryTransId.Value > 0)
                {
                    model.InventoryTransId = routeInventoryTransId;
                }

                return;
            }

            if (!model.InventoryTransId.HasValue && routeInventoryTransId.HasValue && routeInventoryTransId.Value > 0)
            {
                model.InventoryTransId = routeInventoryTransId;
            }

            var postedType = Request.Form["Type"].ToString();
            if (!string.IsNullOrWhiteSpace(postedType))
            {
                model.Type = postedType;
            }

            if (!model.SupplierId.HasValue && int.TryParse(Request.Form["SupplierId"].ToString(), out var supplierId))
            {
                model.SupplierId = supplierId;
            }

            if (!model.InventoryTransId.HasValue &&
                int.TryParse(Request.Form["InventoryTransId"].ToString(), out var postedInventoryTransId) &&
                postedInventoryTransId > 0)
            {
                model.InventoryTransId = postedInventoryTransId;
            }
        }

        private async Task<IActionResult> BuildTransactionFormErrorResultAsync(AdminStockTransactionFormViewModel model, string message)
        {
            model.Suppliers = await GetSupplierOptionsAsync();
            model.Variants = model.Products.FirstOrDefault()?.Variants ?? model.Variants;

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = false,
                    message,
                    debug = BuildTransactionFormPostDebug(model)
                });
            }

            TempData["error"] = message;
            return View(model.IsEdit ? "EditTransactionForm" : "CreateTransactionForm", model);
        }

        private static string GetRootExceptionMessage(Exception exception)
        {
            var current = exception;
            while (current.InnerException != null)
            {
                current = current.InnerException;
            }

            return current.Message;
        }

        private object BuildTransactionFormPostDebug(AdminStockTransactionFormViewModel model)
        {
            return new
            {
                type = model.Type,
                supplierId = model.SupplierId,
                postedSupplierId = Request.HasFormContentType ? Request.Form["SupplierId"].ToString() : string.Empty,
                productCount = model.Products.Count,
                variantCount = model.Products.Sum(product => product.Variants.Count),
                positiveVariantCount = model.Products.Sum(product => product.Variants.Count(variant => variant.Quantity > 0)),
                payloadJsonLength = model.ProductPayloadJson?.Length ?? 0
            };
        }

        private string BuildTransactionReturnUrl(AdminStockTransactionFormViewModel model)
        {
            return "/Admin/StockManager/Transactions";
        }

        private async Task<AdminStockTransactionFormViewModel?> BuildTransactionFormModelAsync(int? productId, int? inventoryTransactionId)
        {
            if (inventoryTransactionId.HasValue)
            {
                var transaction = await _context.InventoryTransactions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.InventoryTransId == inventoryTransactionId.Value);

                if (transaction == null)
                {
                    return null;
                }

                var transactions = await GetInventoryTransactionGroupQuery(transaction)
                    .Include(x => x.Product)
                    .ThenInclude(x => x.VarientProducts)
                    .Include(x => x.InventoryTransactionsDetail)
                    .OrderBy(x => x.InventoryTransId)
                    .ToListAsync();

                if (transactions.Count == 0)
                {
                    return null;
                }

                var primary = transactions[0];
                var products = transactions
                    .OrderBy(x => x.Product.Name)
                    .Select(x => BuildTransactionProductViewModel(x.Product, x.InventoryTransactionsDetail))
                    .ToList();

                return new AdminStockTransactionFormViewModel
                {
                    InventoryTransId = primary.InventoryTransId,
                    ProductId = primary.ProductId,
                    ProductName = products.FirstOrDefault()?.ProductName ?? primary.Product.Name,
                    ProductSku = products.FirstOrDefault()?.ProductSku ?? primary.Product.Sku ?? string.Empty,
                    ProductImageUrl = products.FirstOrDefault()?.ProductImageUrl ?? primary.Product.Image,
                    Type = primary.Type,
                    SupplierId = primary.SupplierId,
                    Note = primary.Note,
                    Suppliers = await GetSupplierOptionsAsync(),
                    Products = products,
                    Variants = products.FirstOrDefault()?.Variants ?? new List<AdminStockTransactionVariantViewModel>()
                };
            }

            if (!productId.HasValue)
            {
                return null;
            }

            var product = await _context.Products
                .Include(x => x.VarientProducts)
                .FirstOrDefaultAsync(x => x.ProductId == productId.Value);

            if (product == null)
            {
                return null;
            }

            var productModel = BuildTransactionProductViewModel(product, Array.Empty<InventoryTransactionsDetail>());

            return new AdminStockTransactionFormViewModel
            {
                ProductId = product.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku ?? string.Empty,
                ProductImageUrl = product.Image,
                Type = "Import",
                Suppliers = await GetSupplierOptionsAsync(),
                Products = new List<AdminStockBatchImportProductViewModel> { productModel },
                Variants = productModel.Variants
            };
        }

        private async Task<AdminStockBatchImportFormViewModel> BuildBatchImportFormModelAsync(int[] selectedProductIds)
        {
            var products = await _context.Products
                .Include(x => x.VarientProducts)
                .Where(x =>
                    selectedProductIds.Contains(x.ProductId) &&
                    x.Visible == true &&
                    x.Status != "discontinued")
                .OrderBy(x => x.Name)
                .ToListAsync();

            return new AdminStockBatchImportFormViewModel
            {
                Suppliers = await GetSupplierOptionsAsync(),
                Products = products.Select(product => new AdminStockBatchImportProductViewModel
                {
                    ProductId = product.ProductId,
                    ProductName = product.Name,
                    ProductSku = product.Sku ?? string.Empty,
                    ProductImageUrl = product.Image,
                    Variants = product.VarientProducts
                        .OrderBy(x => x.Sku)
                        .Select(x => new AdminStockTransactionVariantViewModel
                        {
                            VariantId = x.VarientId,
                            Sku = x.Sku,
                            AttributeSummary = x.Attributes ?? "N/A",
                            Price = x.Price,
                            CurrentStock = x.Stock ?? 0,
                            Quantity = 0
                        })
                        .ToList()
                }).ToList()
            };
        }

        private async Task<IReadOnlyList<AdminStockSupplierOptionViewModel>> GetSupplierOptionsAsync()
        {
            return await _context.Suppliers
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new AdminStockSupplierOptionViewModel
                {
                    SupplierId = x.SupplierId,
                    Code = x.Code,
                    Name = x.Name
                })
                .ToListAsync();
        }

        private async Task RevertInventoryTransactionAsync(Product product, string oldType, IEnumerable<InventoryTransactionsDetail> oldDetails)
        {
            foreach (var detail in oldDetails)
            {
                var variant = product.VarientProducts.FirstOrDefault(x => x.VarientId == detail.VarientId);
                if (variant == null)
                {
                    variant = await _context.VarientProducts.FirstOrDefaultAsync(x => x.VarientId == detail.VarientId);
                    if (variant == null)
                    {
                        continue;
                    }
                }

                if (string.Equals(oldType, "Import", StringComparison.OrdinalIgnoreCase))
                {
                    product.Stock -= detail.Quantity;
                    variant.Stock = (variant.Stock ?? 0) - detail.Quantity;
                }
                else
                {
                    product.Stock += detail.Quantity;
                    variant.Stock = (variant.Stock ?? 0) + detail.Quantity;
                }
            }
        }

        private static void EnsureNonNegativeStock(IEnumerable<Product> products)
        {
            foreach (var product in products)
            {
                if (product.Stock < 0)
                {
                    throw new InvalidOperationException($"Tồn kho của sản phẩm {product.Name} không đủ để thực hiện thao tác này.");
                }

                foreach (var variant in product.VarientProducts)
                {
                    if ((variant.Stock ?? 0) < 0)
                    {
                        throw new InvalidOperationException($"Tồn kho của biến thể {variant.Sku} trong sản phẩm {product.Name} không đủ để thực hiện thao tác này.");
                    }
                }
            }
        }

        private static string ResolveStockStatus(int stock, string? currentStatus)
        {
            if (stock <= 0)
            {
                return "outstock";
            }

            if (string.Equals(currentStatus, "discontinued", StringComparison.OrdinalIgnoreCase))
            {
                return "discontinued";
            }

            if (string.Equals(currentStatus, "preorder", StringComparison.OrdinalIgnoreCase))
            {
                return "preorder";
            }

            return "available";
        }

        private static void CopyTransactionReturnState(AdminStockTransactionFormViewModel source, AdminStockTransactionFormViewModel target)
        {
            target.ReturnPage = source.ReturnPage;
            target.ReturnPageSize = source.ReturnPageSize;
            target.ReturnSku = source.ReturnSku;
            target.ReturnName = source.ReturnName;
            target.ReturnStatus = source.ReturnStatus;
            target.ReturnCategoryId = source.ReturnCategoryId;
            target.ReturnBrandId = source.ReturnBrandId;
            target.ReturnStockFrom = source.ReturnStockFrom;
            target.ReturnStockTo = source.ReturnStockTo;
            target.ReturnSource = source.ReturnSource;
            target.ReturnHistoryFilterCode = source.ReturnHistoryFilterCode;
            target.ReturnHistoryFilterType = source.ReturnHistoryFilterType;
            target.ReturnHistoryStartDate = source.ReturnHistoryStartDate;
            target.ReturnHistoryEndDate = source.ReturnHistoryEndDate;
        }

        private static string BuildInventoryTransactionGroupKey(InventoryTransactionsVM item)
        {
            return string.Join("|",
                item.CreatedAt.Ticks,
                item.UserId,
                item.Type,
                item.SupplierId?.ToString() ?? "null",
                item.Note ?? string.Empty);
        }

        private static List<AdminStockBatchImportProductViewModel> ParseBatchTransactionProducts(string payloadJson)
        {
            var products = new List<AdminStockBatchImportProductViewModel>();

            if (string.IsNullOrWhiteSpace(payloadJson))
            {
                return products;
            }

            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return products;
            }

            foreach (var productElement in document.RootElement.EnumerateArray())
            {
                var variants = new List<AdminStockTransactionVariantViewModel>();
                if (TryGetProperty(productElement, "variants", out var variantsElement) &&
                    variantsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var variantElement in variantsElement.EnumerateArray())
                    {
                        variants.Add(new AdminStockTransactionVariantViewModel
                        {
                            VariantId = ReadInt32(variantElement, "variantId"),
                            Sku = ReadString(variantElement, "sku"),
                            AttributeSummary = ReadString(variantElement, "attributeSummary"),
                            Price = ReadNullableDecimal(variantElement, "price"),
                            CurrentStock = ReadInt32(variantElement, "currentStock"),
                            PreviousQuantity = ReadInt32(variantElement, "previousQuantity"),
                            Quantity = ReadInt32(variantElement, "quantity")
                        });
                    }
                }

                products.Add(new AdminStockBatchImportProductViewModel
                {
                    ProductId = ReadInt32(productElement, "productId"),
                    ProductName = ReadString(productElement, "productName"),
                    ProductSku = ReadString(productElement, "productSku"),
                    ProductImageUrl = ReadNullableString(productElement, "productImageUrl"),
                    Variants = variants
                });
            }

            return products;
        }

        private static List<AdminStockBatchImportProductViewModel> ResolveBatchTransactionProducts(AdminStockBatchImportFormViewModel model)
        {
            var boundProducts = model.Products ?? new List<AdminStockBatchImportProductViewModel>();
            var jsonProducts = new List<AdminStockBatchImportProductViewModel>();

            if (!string.IsNullOrWhiteSpace(model.ProductPayloadJson))
            {
                try
                {
                    jsonProducts = ParseBatchTransactionProducts(model.ProductPayloadJson);
                }
                catch (JsonException)
                {
                    jsonProducts = new List<AdminStockBatchImportProductViewModel>();
                }
            }

            if (HasPositiveQuantity(jsonProducts))
            {
                return jsonProducts;
            }

            if (HasPositiveQuantity(boundProducts))
            {
                return boundProducts;
            }

            return jsonProducts.Count > 0 ? jsonProducts : boundProducts;
        }

        private static bool HasPositiveQuantity(IEnumerable<AdminStockBatchImportProductViewModel> products)
        {
            return products.Any(product => product.Variants.Any(variant => variant.Quantity > 0));
        }

        private static bool TryGetProperty(JsonElement element, string propertyName, out JsonElement value)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    value = property.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static int ReadInt32(JsonElement element, string propertyName)
        {
            if (!TryGetProperty(element, propertyName, out var value))
            {
                return 0;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }

            return 0;
        }

        private static decimal? ReadNullableDecimal(JsonElement element, string propertyName)
        {
            if (!TryGetProperty(element, propertyName, out var value))
            {
                return null;
            }

            if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number))
            {
                return number;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(value.GetString(), out var parsed))
            {
                return parsed;
            }

            return null;
        }

        private static string ReadString(JsonElement element, string propertyName)
        {
            return ReadNullableString(element, propertyName) ?? string.Empty;
        }

        private static string? ReadNullableString(JsonElement element, string propertyName)
        {
            if (!TryGetProperty(element, propertyName, out var value))
            {
                return null;
            }

            return value.ValueKind == JsonValueKind.Null ? null : value.ToString();
        }

        private bool IsAjaxRequest()
        {
            return string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        }

        private async Task<IActionResult> BuildBatchTransactionErrorResultAsync(AdminStockBatchImportFormViewModel model, string message)
        {
            model.Suppliers = await GetSupplierOptionsAsync();

            if (IsAjaxRequest())
            {
                return Json(new
                {
                    success = false,
                    message,
                    debug = BuildBatchTransactionPostDebug(model)
                });
            }

            TempData["error"] = message;
            return View("BatchImport", model);
        }

        private static object BuildBatchTransactionPostDebug(AdminStockBatchImportFormViewModel model)
        {
            var products = model.Products ?? new List<AdminStockBatchImportProductViewModel>();

            return new
            {
                productCount = products.Count,
                variantCount = products.Sum(product => product.Variants.Count),
                positiveVariantCount = products.Sum(product => product.Variants.Count(variant => variant.Quantity > 0)),
                payloadJsonLength = model.ProductPayloadJson?.Length ?? 0
            };
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

        [HttpPost("ExportToExcel")]
        public IActionResult ExportToExcel([FromBody] string[] VariantIds)
        {
            // Lấy danh sách biến thể từ database
            var variants = _context.VarientProducts
                .Include(x => x.Product)
                .ThenInclude(p => p.Category)
                .Where(x => VariantIds.Contains(x.VarientId.ToString()))
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
