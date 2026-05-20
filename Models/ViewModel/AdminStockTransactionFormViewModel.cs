namespace Tech_Store.Models.ViewModel;

public class AdminStockTransactionFormViewModel
{
    public int? InventoryTransId { get; set; }

    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public string Type { get; set; } = "Import";

    public int? SupplierId { get; set; }

    public string? Note { get; set; }

    public IReadOnlyList<AdminStockSupplierOptionViewModel> Suppliers { get; set; } = Array.Empty<AdminStockSupplierOptionViewModel>();

    public List<AdminStockTransactionVariantViewModel> Variants { get; set; } = new();

    public List<AdminStockBatchImportProductViewModel> Products { get; set; } = new();

    public string? ProductPayloadJson { get; set; }

    public bool IsEdit => InventoryTransId.HasValue && InventoryTransId.Value > 0;

    public int ReturnPage { get; set; } = 1;

    public int ReturnPageSize { get; set; }

    public string? ReturnSku { get; set; }

    public string? ReturnName { get; set; }

    public string? ReturnStatus { get; set; }

    public int? ReturnCategoryId { get; set; }

    public int? ReturnBrandId { get; set; }

    public int? ReturnStockFrom { get; set; }

    public int? ReturnStockTo { get; set; }

    public string ReturnSource { get; set; } = "stock-index";

    public string? ReturnHistoryFilterCode { get; set; }

    public string? ReturnHistoryFilterType { get; set; }

    public DateOnly? ReturnHistoryStartDate { get; set; }

    public DateOnly? ReturnHistoryEndDate { get; set; }
}
