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
using Tech_Store.Models.ViewModel;
namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class StockManagerController : BaseAdminController
    {
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;

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

        [Route("")]
        [Route("index")]
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

            return View("TransactionForm", model);
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

            return View("TransactionForm", model);
        }

        [HttpPost("SaveTransaction")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveTransaction(AdminStockTransactionFormViewModel model)
        {
            var validVariants = model.Variants
                .Where(x => x.Quantity > 0)
                .ToList();

            if (!validVariants.Any())
            {
                TempData["error"] = "Cần nhập ít nhất một biến thể có số lượng lớn hơn 0.";
                var invalidModel = await BuildTransactionFormModelAsync(model.ProductId, model.InventoryTransId);
                if (invalidModel == null)
                {
                    return NotFound();
                }

                invalidModel.Type = model.Type;
                invalidModel.SupplierId = model.SupplierId;
                invalidModel.Note = model.Note;
                invalidModel.Variants = invalidModel.Variants.Select(x =>
                {
                    var input = validVariants.FirstOrDefault(v => v.VariantId == x.VariantId) ?? model.Variants.FirstOrDefault(v => v.VariantId == x.VariantId);
                    x.Quantity = input?.Quantity ?? 0;
                    return x;
                }).ToList();
                CopyTransactionReturnState(model, invalidModel);
                return View("TransactionForm", invalidModel);
            }

            if (string.Equals(model.Type, "Import", StringComparison.OrdinalIgnoreCase) && !model.SupplierId.HasValue)
            {
                TempData["error"] = "Phiếu nhập kho cần gắn với một nhà cung cấp.";
                var invalidModel = await BuildTransactionFormModelAsync(model.ProductId, model.InventoryTransId);
                if (invalidModel == null)
                {
                    return NotFound();
                }

                invalidModel.Type = model.Type;
                invalidModel.SupplierId = model.SupplierId;
                invalidModel.Note = model.Note;
                invalidModel.Variants = invalidModel.Variants.Select(x =>
                {
                    var input = model.Variants.FirstOrDefault(v => v.VariantId == x.VariantId);
                    x.Quantity = input?.Quantity ?? 0;
                    return x;
                }).ToList();
                CopyTransactionReturnState(model, invalidModel);
                return View("TransactionForm", invalidModel);
            }

            await using var dbTransaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var product = await _context.Products
                    .Include(x => x.VarientProducts)
                    .FirstOrDefaultAsync(x => x.ProductId == model.ProductId);

                if (product == null)
                {
                    return NotFound();
                }

                InventoryTransactions inventoryTransaction;
                List<InventoryTransactionsDetail> oldDetails = new();

                if (model.InventoryTransId.HasValue && model.InventoryTransId.Value > 0)
                {
                    inventoryTransaction = await _context.InventoryTransactions
                        .Include(x => x.InventoryTransactionsDetail)
                        .FirstOrDefaultAsync(x => x.InventoryTransId == model.InventoryTransId.Value);

                    if (inventoryTransaction == null)
                    {
                        return NotFound();
                    }

                    oldDetails = inventoryTransaction.InventoryTransactionsDetail.ToList();
                    await RevertInventoryTransactionAsync(product, inventoryTransaction.Type, oldDetails);

                    _context.InventoryTransactionsDetail.RemoveRange(oldDetails);
                }
                else
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    inventoryTransaction = new InventoryTransactions
                    {
                        ProductId = product.ProductId,
                        UserId = int.Parse(userId!),
                        CreatedAt = DateTime.Now
                    };
                    await _context.InventoryTransactions.AddAsync(inventoryTransaction);
                }

                var detailEntities = new List<InventoryTransactionsDetail>();
                var totalQuantity = 0;

                foreach (var variantInput in validVariants)
                {
                    var variant = product.VarientProducts.FirstOrDefault(x => x.VarientId == variantInput.VariantId);
                    if (variant == null)
                    {
                        throw new InvalidOperationException($"Không tìm thấy biến thể {variantInput.VariantId}.");
                    }

                    if (string.Equals(model.Type, "Import", StringComparison.OrdinalIgnoreCase))
                    {
                        product.Stock += variantInput.Quantity;
                        variant.Stock = (variant.Stock ?? 0) + variantInput.Quantity;
                    }
                    else
                    {
                        if ((variant.Stock ?? 0) < variantInput.Quantity || product.Stock < variantInput.Quantity)
                        {
                            throw new InvalidOperationException($"Tồn kho của biến thể {variant.Sku} không đủ để xuất.");
                        }

                        product.Stock -= variantInput.Quantity;
                        variant.Stock = (variant.Stock ?? 0) - variantInput.Quantity;
                    }

                    totalQuantity += variantInput.Quantity;

                    detailEntities.Add(new InventoryTransactionsDetail
                    {
                        InventoryTransId = inventoryTransaction.InventoryTransId,
                        VarientId = variant.VarientId,
                        Quantity = variantInput.Quantity
                    });
                }

                if (!string.Equals(model.Type, "Import", StringComparison.OrdinalIgnoreCase) && product.Stock < 0)
                {
                    throw new InvalidOperationException("Tồn kho tổng của sản phẩm không đủ để xuất.");
                }

                inventoryTransaction.Type = model.Type;
                inventoryTransaction.SupplierId = string.Equals(model.Type, "Import", StringComparison.OrdinalIgnoreCase)
                    ? model.SupplierId
                    : null;
                inventoryTransaction.Note = model.Note;
                inventoryTransaction.UpdatedAt = DateTime.Now;
                inventoryTransaction.ProductId = product.ProductId;

                await _context.SaveChangesAsync();

                foreach (var detail in detailEntities)
                {
                    detail.InventoryTransId = inventoryTransaction.InventoryTransId;
                }

                await _context.InventoryTransactionsDetail.AddRangeAsync(detailEntities);

                product.Status = ResolveStockStatus(product.Stock, product.Status);

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                TempData["success"] = model.InventoryTransId.HasValue ? "Cập nhật phiếu kho thành công." : "Tạo phiếu kho thành công.";

                if (string.Equals(model.ReturnSource, "history", StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(nameof(History), new
                    {
                        startDate = model.ReturnHistoryStartDate,
                        endDate = model.ReturnHistoryEndDate,
                        filterType = model.ReturnHistoryFilterType,
                        filterCode = model.ReturnHistoryFilterCode,
                        page = model.ReturnPage,
                        pageSize = model.ReturnPageSize
                    });
                }

                return RedirectToAction(nameof(Index), new
                {
                    page = model.ReturnPage,
                    sku = model.ReturnSku,
                    name = model.ReturnName,
                    status = model.ReturnStatus,
                    categoryId = model.ReturnCategoryId,
                    brandId = model.ReturnBrandId,
                    stockFrom = model.ReturnStockFrom,
                    stockTo = model.ReturnStockTo
                });
            }
            catch (Exception ex)
            {
                await dbTransaction.RollbackAsync();
                TempData["error"] = ex.Message;

                var invalidModel = await BuildTransactionFormModelAsync(model.ProductId, model.InventoryTransId);
                if (invalidModel == null)
                {
                    return NotFound();
                }

                invalidModel.Type = model.Type;
                invalidModel.SupplierId = model.SupplierId;
                invalidModel.Note = model.Note;
                invalidModel.Variants = invalidModel.Variants.Select(x =>
                {
                    var input = model.Variants.FirstOrDefault(v => v.VariantId == x.VariantId);
                    x.Quantity = input?.Quantity ?? 0;
                    return x;
                }).ToList();
                CopyTransactionReturnState(model, invalidModel);
                return View("TransactionForm", invalidModel);
            }
        }

        [Route("History")]
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

            var importCount = query.Count(ph => ph.Type == "Import");
            var exportCount = query.Count(ph => ph.Type == "Export");
            var totalItems = query.Count();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);
            var currentPage = Math.Min(Math.Max(page, 1), totalPages);

            var history = query
                .OrderByDescending(ph => ph.InventoryTransId)
                .Skip((currentPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .Select(ph => new InventoryTransactionsVM
                {
                    Id = ph.InventoryTransId,
                    Product = ph.Product,
                    InventoryTransId = ph.InventoryTransId,
                    Type = ph.Type,
                    Note = ph.Note,
                    CreatedAt = ph.CreatedAt ?? DateTime.MinValue,
                    UserName = ph.User.LastName + " " + ph.User.FirstName,
                    UserRole = ph.User.Roles.FirstOrDefault().RoleName,
                    SupplierName = ph.Supplier != null ? ph.Supplier.Name : null,
                    SupplierCode = ph.Supplier != null ? ph.Supplier.Code : null,
                    InventoryTransactionDetail = ph.InventoryTransactionsDetail
                        .Select(d => new InventorTransactionDetailViewModel
                        {
                            Quantity = d.Quantity
                        })
                        .ToList()
                })
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
            var history = await _context.InventoryTransactions
                .Where(ph => ph.InventoryTransId == id) // Điều kiện lọc trước khi Select
                .Include(p => p.Product)
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .ThenInclude(p => p.Roles)
                .Include(p => p.InventoryTransactionsDetail).ThenInclude(p => p.Varient)
                .Select(ph => new InventoryTransactionsVM
                {
                    Id = ph.InventoryTransId,
                    Product = ph.Product,
                    Type = ph.Type,
                    Note = ph.Note,
                    CreatedAt = (DateTime)ph.CreatedAt,
                    InventoryTransactionDetail = ph.InventoryTransactionsDetail.Select(d => new InventorTransactionDetailViewModel
                    {
                        InventoryTransId = d.InventoryTransId,
                        VarientId = d.VarientId,
                        VarientSku = d.Varient.Sku,
                        Quantity = d.Quantity,
                        VarientName = d.Varient.Attributes // Optional, if needed
                    }).ToList(), // No need to cast here
                    UserName = ph.User.LastName + " " + ph.User.FirstName,
                    UserRole = ph.User.Roles.FirstOrDefault().RoleName ?? "Không có vai trò", // Tránh lỗi null
                    SupplierName = ph.Supplier != null ? ph.Supplier.Name : null,
                    SupplierCode = ph.Supplier != null ? ph.Supplier.Code : null
                })
                .FirstOrDefaultAsync();

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

        private async Task<AdminStockTransactionFormViewModel?> BuildTransactionFormModelAsync(int? productId, int? inventoryTransactionId)
        {
            InventoryTransactions? transaction = null;
            Product? product;

            if (inventoryTransactionId.HasValue)
            {
                transaction = await _context.InventoryTransactions
                    .Include(x => x.Product)
                    .ThenInclude(x => x.VarientProducts)
                    .Include(x => x.InventoryTransactionsDetail)
                    .FirstOrDefaultAsync(x => x.InventoryTransId == inventoryTransactionId.Value);

                if (transaction == null || transaction.Product == null)
                {
                    return null;
                }

                product = transaction.Product;
            }
            else
            {
                if (!productId.HasValue)
                {
                    return null;
                }

                product = await _context.Products
                    .Include(x => x.VarientProducts)
                    .FirstOrDefaultAsync(x => x.ProductId == productId.Value);

                if (product == null)
                {
                    return null;
                }
            }

            var suppliers = await _context.Suppliers
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new AdminStockSupplierOptionViewModel
                {
                    SupplierId = x.SupplierId,
                    Code = x.Code,
                    Name = x.Name
                })
                .ToListAsync();

            var detailLookup = transaction?.InventoryTransactionsDetail.ToDictionary(x => x.VarientId, x => x.Quantity)
                              ?? new Dictionary<int, int>();

            return new AdminStockTransactionFormViewModel
            {
                InventoryTransId = transaction?.InventoryTransId,
                ProductId = product.ProductId,
                ProductName = product.Name,
                ProductSku = product.Sku ?? string.Empty,
                ProductImageUrl = product.Image,
                Type = transaction?.Type ?? "Import",
                SupplierId = transaction?.SupplierId,
                Note = transaction?.Note,
                Suppliers = suppliers,
                Variants = product.VarientProducts
                    .OrderBy(x => x.Sku)
                    .Select(x => new AdminStockTransactionVariantViewModel
                    {
                        VariantId = x.VarientId,
                        Sku = x.Sku,
                        AttributeSummary = x.Attributes ?? "N/A",
                        Price = x.Price,
                        CurrentStock = x.Stock ?? 0,
                        Quantity = detailLookup.TryGetValue(x.VarientId, out var quantity) ? quantity : 0
                    })
                    .ToList()
            };
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
