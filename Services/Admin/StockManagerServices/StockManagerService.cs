using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Services.Admin.StockManagerServices;

public class StockManagerService : IStockManagerService
{
    private static readonly JsonSerializerOptions PayloadJsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ApplicationDbContext _context;

    public StockManagerService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AdminStockTransactionFormViewModel?> GetEditTransactionFormAsync(int inventoryTransactionId)
    {
        return BuildEditTransactionFormAsync(inventoryTransactionId);
    }

    public async Task PrepareTransactionFormAsync(AdminStockTransactionFormViewModel model)
    {
        NormalizeTransactionForm(model);
        model.Suppliers = await GetSupplierOptionsAsync();
        model.Variants = model.Products.FirstOrDefault()?.Variants ?? model.Variants;
    }

    public async Task CreateTransactionAsync(AdminStockBatchImportFormViewModel model, int userId)
    {
        NormalizeBatchTransactionForm(model);
        await CreateTransactionCoreAsync(model, userId);
    }

    public async Task UpdateTransactionAsync(int inventoryTransactionId, AdminStockTransactionFormViewModel model, int userId)
    {
        NormalizeTransactionForm(model);
        await SaveTransactionAsync(model, userId, inventoryTransactionId);
    }

    public async Task DeleteTransactionAsync(int inventoryTransactionId)
    {
        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var seedTransaction = await _context.InventoryTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InventoryTransId == inventoryTransactionId);

        if (seedTransaction == null)
        {
            throw new InvalidOperationException("Không tìm thấy phiếu kho cần xóa.");
        }

        var existingTransactions = await GetInventoryTransactionGroupQuery(seedTransaction)
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
        _context.InventoryTransactions.RemoveRange(existingTransactions);
        await _context.SaveChangesAsync();
        EnsureNonNegativeStock(existingTransactions.Select(x => x.Product).DistinctBy(x => x.ProductId));
        await dbTransaction.CommitAsync();
    }

    public async Task<AdminStockBatchImportFormViewModel> GetBatchTransactionFormAsync(int[] selectedProductIds)
    {
        if (selectedProductIds == null || selectedProductIds.Length == 0)
        {
            return new AdminStockBatchImportFormViewModel
            {
                Suppliers = await GetSupplierOptionsAsync()
            };
        }

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
            Products = products.Select(BuildBatchProductViewModel).ToList()
        };
    }

    public async Task PrepareBatchTransactionFormAsync(AdminStockBatchImportFormViewModel model)
    {
        NormalizeBatchTransactionForm(model);
        model.Suppliers = await GetSupplierOptionsAsync();
    }

    public async Task<IReadOnlyList<AdminStockBatchImportProductViewModel>> GetBatchTransactionProductsAsync(int[] selectedProductIds)
    {
        var model = await GetBatchTransactionFormAsync(selectedProductIds);
        return model.Products;
    }

    private async Task CreateTransactionCoreAsync(AdminStockBatchImportFormViewModel model, int userId)
    {
        NormalizeBatchTransactionForm(model);

        var validProducts = model.Products
            .Select(product => new
            {
                Product = product,
                Variants = product.Variants.Where(variant => variant.Quantity > 0).ToList()
            })
            .Where(x => x.Variants.Count > 0)
            .ToList();

        var isImport = IsImport(model.Type);
        var isExport = IsExport(model.Type);
        if (!isImport && !isExport)
        {
            throw new InvalidOperationException("Loại phiếu kho không hợp lệ.");
        }

        if (isImport && !model.SupplierId.HasValue)
        {
            throw new InvalidOperationException("Phiếu nhập kho cần gắn với một nhà cung cấp.");
        }

        if (validProducts.Count == 0)
        {
            throw new InvalidOperationException("Cần nhập ít nhất một biến thể có số lượng lớn hơn 0.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var createdAt = DateTime.Now;
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
                Type = NormalizeTransactionType(model.Type),
                UserId = userId,
                SupplierId = isImport ? model.SupplierId : null,
                Note = model.Note,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            };

            await _context.InventoryTransactions.AddAsync(inventoryTransaction);
            await _context.SaveChangesAsync();

            var details = BuildDetailEntities(product, productInput.Variants, inventoryTransaction.InventoryTransId, isImport);
            product.Status = ResolveStockStatus(product.Stock, product.Status);
            await _context.InventoryTransactionsDetail.AddRangeAsync(details);
            await _context.SaveChangesAsync();
        }

        EnsureNonNegativeStock(products.Values);
        await dbTransaction.CommitAsync();
    }

    public async Task<AdminStockHistoryIndexViewModel> GetHistoryAsync(DateOnly? startDate, DateOnly? endDate, string? filterType, string? filterCode, int page, int pageSize)
    {
        var query = _context.InventoryTransactions
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.InventoryTransactionsDetail)
            .Include(x => x.Supplier)
            .Include(x => x.User)
            .ThenInclude(x => x.Roles)
            .AsQueryable();

        if (startDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt >= startDate.Value.ToDateTime(TimeOnly.MinValue));
        }

        if (endDate.HasValue)
        {
            query = query.Where(x => x.CreatedAt <= endDate.Value.ToDateTime(TimeOnly.MaxValue));
        }

        if (!string.IsNullOrWhiteSpace(filterType))
        {
            var normalizedType = NormalizeTransactionType(filterType);
            query = query.Where(x => x.Type == normalizedType);
        }

        if (!string.IsNullOrWhiteSpace(filterCode))
        {
            var normalizedCode = filterCode.Trim();
            query = query.Where(x =>
                x.Product.Sku.Contains(normalizedCode) ||
                x.Product.Name.Contains(normalizedCode) ||
                x.InventoryTransId.ToString().Contains(normalizedCode));
        }

        var rawHistorySource = await query
            .OrderByDescending(x => x.InventoryTransId)
            .ToListAsync();
        var rawHistory = rawHistorySource
            .Select(MapHistoryRow)
            .ToList();

        var groupedHistory = GroupHistory(rawHistory);
        var importCount = groupedHistory.Count(x => x.Type == "Import");
        var exportCount = groupedHistory.Count(x => x.Type == "Export");
        var totalItems = groupedHistory.Count;
        var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)pageSize);
        var currentPage = Math.Min(Math.Max(page, 1), totalPages);

        return new AdminStockHistoryIndexViewModel
        {
            Items = groupedHistory
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToList(),
            FilterCode = filterCode,
            FilterType = filterType,
            StartDate = startDate,
            EndDate = endDate,
            Page = currentPage,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalItems = totalItems,
            ImportCount = importCount,
            ExportCount = exportCount,
            QueryString = BuildHistoryQueryString(startDate, endDate, filterType, filterCode, pageSize)
        };
    }

    public async Task<InventoryTransactionsVM?> GetHistoryDetailAsync(int id)
    {
        var selectedTransaction = await _context.InventoryTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InventoryTransId == id);

        if (selectedTransaction == null)
        {
            return null;
        }

        var histories = await GetInventoryTransactionGroupQuery(selectedTransaction)
            .AsNoTracking()
            .Include(x => x.Product)
            .Include(x => x.Supplier)
            .Include(x => x.User)
            .ThenInclude(x => x.Roles)
            .Include(x => x.InventoryTransactionsDetail)
            .ThenInclude(x => x.Varient)
            .OrderByDescending(x => x.InventoryTransId)
            .Select(x => new InventoryTransactionsVM
            {
                Id = x.InventoryTransId,
                InventoryTransId = x.InventoryTransId,
                UserId = x.UserId,
                SupplierId = x.SupplierId,
                Product = x.Product,
                Type = x.Type,
                Note = x.Note ?? string.Empty,
                CreatedAt = x.CreatedAt ?? DateTime.MinValue,
                ProductCount = 1,
                TotalQuantity = x.InventoryTransactionsDetail.Sum(d => d.Quantity),
                UserName = x.User.LastName + " " + x.User.FirstName,
                UserRole = x.User.Roles.FirstOrDefault().RoleName ?? "Không có vai trò",
                SupplierName = x.Supplier != null ? x.Supplier.Name : null,
                SupplierCode = x.Supplier != null ? x.Supplier.Code : null,
                InventoryTransactionDetail = x.InventoryTransactionsDetail.Select(d => new InventorTransactionDetailViewModel
                {
                    InventoryTransId = d.InventoryTransId,
                    VarientId = d.VarientId,
                    VarientSku = d.Varient.Sku,
                    Quantity = d.Quantity,
                    VarientName = d.Varient.Attributes ?? "N/A"
                }).ToList(),
                Products = new List<InventoryTransactionProductSummaryViewModel>
                {
                    new()
                    {
                        InventoryTransId = x.InventoryTransId,
                        ProductId = x.ProductId,
                        ProductName = x.Product.Name,
                        ProductSku = x.Product.Sku ?? string.Empty,
                        ProductImageUrl = x.Product.Image,
                        TotalQuantity = x.InventoryTransactionsDetail.Sum(d => d.Quantity),
                        Details = x.InventoryTransactionsDetail.Select(d => new InventorTransactionDetailViewModel
                        {
                            InventoryTransId = d.InventoryTransId,
                            VarientId = d.VarientId,
                            VarientSku = d.Varient.Sku,
                            Quantity = d.Quantity,
                            VarientName = d.Varient.Attributes ?? "N/A"
                        }).ToList()
                    }
                }
            })
            .ToListAsync();

        return GroupHistory(histories).FirstOrDefault();
    }

    private async Task SaveTransactionAsync(AdminStockTransactionFormViewModel model, int userId, int? inventoryTransactionId)
    {
        var isUpdate = inventoryTransactionId.HasValue && inventoryTransactionId.Value > 0;
        model.InventoryTransId = isUpdate ? inventoryTransactionId : null;

        var validProducts = model.Products
            .Select(product => new
            {
                Product = product,
                Variants = product.Variants.Where(variant => variant.Quantity > 0).ToList()
            })
            .Where(x => x.Variants.Count > 0)
            .ToList();

        var isImport = IsImport(model.Type);
        var isExport = IsExport(model.Type);
        if (!isImport && !isExport)
        {
            throw new InvalidOperationException("Loại phiếu kho không hợp lệ.");
        }

        if (validProducts.Count == 0)
        {
            throw new InvalidOperationException("Cần nhập ít nhất một biến thể có số lượng lớn hơn 0.");
        }

        if (isImport && !model.SupplierId.HasValue)
        {
            throw new InvalidOperationException("Phiếu nhập kho cần gắn với một nhà cung cấp.");
        }

        await using var dbTransaction = await _context.Database.BeginTransactionAsync();

        var createdAt = DateTime.Now;
        var transactionUserId = userId;
        var existingTransactions = new List<InventoryTransactions>();

        if (isUpdate)
        {
            var seedTransaction = await _context.InventoryTransactions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.InventoryTransId == inventoryTransactionId!.Value);

            if (seedTransaction == null)
            {
                throw new InvalidOperationException("Không tìm thấy phiếu kho cần cập nhật.");
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
                    Type = NormalizeTransactionType(model.Type),
                    UserId = transactionUserId,
                    SupplierId = isImport ? model.SupplierId : null,
                    Note = model.Note,
                    CreatedAt = createdAt,
                    UpdatedAt = DateTime.Now
                };

                await _context.InventoryTransactions.AddAsync(inventoryTransaction);
                await _context.SaveChangesAsync();
            }

            var detailEntities = BuildDetailEntities(product, productInput.Variants, inventoryTransaction.InventoryTransId, isImport);

            inventoryTransaction.Type = NormalizeTransactionType(model.Type);
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
    }

    private static List<InventoryTransactionsDetail> BuildDetailEntities(Product product, List<AdminStockTransactionVariantViewModel> variants, int inventoryTransId, bool isImport)
    {
        var details = new List<InventoryTransactionsDetail>();

        foreach (var variantInput in variants)
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

            details.Add(new InventoryTransactionsDetail
            {
                InventoryTransId = inventoryTransId,
                VarientId = variant.VarientId,
                Quantity = variantInput.Quantity,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });
        }

        return details;
    }

    private async Task<AdminStockTransactionFormViewModel?> BuildEditTransactionFormAsync(int inventoryTransactionId)
    {
        var transaction = await _context.InventoryTransactions
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.InventoryTransId == inventoryTransactionId);

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

    private IQueryable<InventoryTransactions> GetInventoryTransactionGroupQuery(InventoryTransactions seedTransaction)
    {
        return _context.InventoryTransactions.Where(x =>
            x.CreatedAt == seedTransaction.CreatedAt &&
            x.UserId == seedTransaction.UserId &&
            x.Type == seedTransaction.Type &&
            x.SupplierId == seedTransaction.SupplierId);
    }

    private static AdminStockBatchImportProductViewModel BuildTransactionProductViewModel(Product product, IEnumerable<InventoryTransactionsDetail> details)
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

    private static AdminStockBatchImportProductViewModel BuildBatchProductViewModel(Product product)
    {
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
                    Quantity = 0
                })
                .ToList()
        };
    }

    private static void NormalizeTransactionForm(AdminStockTransactionFormViewModel model)
    {
        model.Type = NormalizeTransactionType(model.Type);
        model.Note = NormalizeNullableText(model.Note);
        model.Products = ResolveTransactionFormProducts(model);
    }

    private static void NormalizeBatchTransactionForm(AdminStockBatchImportFormViewModel model)
    {
        model.Type = NormalizeTransactionType(model.Type);
        model.Note = NormalizeNullableText(model.Note);
        model.Products = ResolveBatchTransactionProducts(model);
    }

    private async Task<IReadOnlyList<AdminStockSupplierOptionViewModel>> GetSupplierOptionsAsync()
    {
        return await _context.Suppliers
            .AsNoTracking()
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

            if (IsImport(oldType))
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

    private static List<InventoryTransactionsVM> GroupHistory(List<InventoryTransactionsVM> rawHistory)
    {
        return rawHistory
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
    }

    private static InventoryTransactionsVM MapHistoryRow(InventoryTransactions x)
    {
        return new InventoryTransactionsVM
        {
            Id = x.InventoryTransId,
            UserId = x.UserId,
            SupplierId = x.SupplierId,
            Product = x.Product,
            InventoryTransId = x.InventoryTransId,
            Type = x.Type,
            Note = x.Note ?? string.Empty,
            CreatedAt = x.CreatedAt ?? DateTime.MinValue,
            ProductCount = 1,
            TotalQuantity = x.InventoryTransactionsDetail.Sum(d => d.Quantity),
            UserName = x.User.LastName + " " + x.User.FirstName,
            UserRole = x.User.Roles.FirstOrDefault().RoleName ?? "Không có vai trò",
            SupplierName = x.Supplier != null ? x.Supplier.Name : null,
            SupplierCode = x.Supplier != null ? x.Supplier.Code : null,
            Products = new List<InventoryTransactionProductSummaryViewModel>
            {
                new()
                {
                    InventoryTransId = x.InventoryTransId,
                    ProductId = x.ProductId,
                    ProductName = x.Product.Name,
                    ProductSku = x.Product.Sku ?? string.Empty,
                    ProductImageUrl = x.Product.Image,
                    TotalQuantity = x.InventoryTransactionsDetail.Sum(d => d.Quantity)
                }
            },
            InventoryTransactionDetail = x.InventoryTransactionsDetail
                .Select(d => new InventorTransactionDetailViewModel
                {
                    InventoryTransId = d.InventoryTransId,
                    VarientId = d.VarientId,
                    Quantity = d.Quantity
                })
                .ToList()
        };
    }

    private static List<AdminStockBatchImportProductViewModel> ResolveTransactionFormProducts(AdminStockTransactionFormViewModel model)
    {
        var jsonProducts = ParseBatchTransactionProducts(model.ProductPayloadJson);

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
                Variants = model.Variants,
               
            }
        };
    }

    private static List<AdminStockBatchImportProductViewModel> ResolveBatchTransactionProducts(AdminStockBatchImportFormViewModel model)
    {
        var jsonProducts = ParseBatchTransactionProducts(model.ProductPayloadJson);

        if (HasPositiveQuantity(jsonProducts))
        {
            return jsonProducts;
        }

        if (HasPositiveQuantity(model.Products))
        {
            return model.Products;
        }

        return jsonProducts.Count > 0 ? jsonProducts : model.Products;
    }

    private static List<AdminStockBatchImportProductViewModel> ParseBatchTransactionProducts(string? payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new List<AdminStockBatchImportProductViewModel>();
        }

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                return new List<AdminStockBatchImportProductViewModel>();
            }

            var products = new List<AdminStockBatchImportProductViewModel>();
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
        catch (JsonException)
        {
            return new List<AdminStockBatchImportProductViewModel>();
        }
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

        if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out var parsed))
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

        if (value.ValueKind == JsonValueKind.String && decimal.TryParse(value.GetString(), out var parsed))
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

    private static string BuildInventoryTransactionGroupKey(InventoryTransactionsVM item)
    {
        return string.Join("|",
            item.CreatedAt.Ticks,
            item.UserId,
            item.Type,
            item.SupplierId?.ToString() ?? "null");
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

    private static bool IsImport(string? type)
    {
        return string.Equals(type, "Import", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExport(string? type)
    {
        return string.Equals(type, "Export", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeTransactionType(string? type)
    {
        if (IsImport(type))
        {
            return "Import";
        }

        if (IsExport(type))
        {
            return "Export";
        }

        return string.Empty;
    }

    private static string? NormalizeNullableText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
