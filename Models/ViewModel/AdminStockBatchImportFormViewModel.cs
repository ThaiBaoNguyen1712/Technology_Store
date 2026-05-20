namespace Tech_Store.Models.ViewModel;

public class AdminStockBatchImportFormViewModel
{
    public string Type { get; set; } = "Import";

    public int? SupplierId { get; set; }

    public string? Note { get; set; }

    public IReadOnlyList<AdminStockSupplierOptionViewModel> Suppliers { get; set; } = Array.Empty<AdminStockSupplierOptionViewModel>();

    public List<AdminStockBatchImportProductViewModel> Products { get; set; } = new();

    public string? ProductPayloadJson { get; set; }

    public int ReturnPage { get; set; } = 1;

    public string? ReturnSku { get; set; }

    public string? ReturnName { get; set; }

    public string? ReturnStatus { get; set; }

    public int? ReturnCategoryId { get; set; }

    public int? ReturnBrandId { get; set; }

    public int? ReturnStockFrom { get; set; }

    public int? ReturnStockTo { get; set; }
}
