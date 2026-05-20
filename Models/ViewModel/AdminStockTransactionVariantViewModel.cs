namespace Tech_Store.Models.ViewModel;

public class AdminStockTransactionVariantViewModel
{
    public int VariantId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string AttributeSummary { get; set; } = string.Empty;

    public decimal? Price { get; set; }

    public int CurrentStock { get; set; }

    public int PreviousQuantity { get; set; }

    public int Quantity { get; set; }
}
