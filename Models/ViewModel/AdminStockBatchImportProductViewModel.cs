namespace Tech_Store.Models.ViewModel;

public class AdminStockBatchImportProductViewModel
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string ProductSku { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public List<AdminStockTransactionVariantViewModel> Variants { get; set; } = new();
}
