namespace Tech_Store.Models.ViewModel;

public class AdminStockExportVariantItemViewModel
{
    public int VariantId { get; set; }

    public string? ImageUrl { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string AttributeSummary { get; set; } = string.Empty;

    public decimal? SellPrice { get; set; }

    public int Stock { get; set; }

    public string? CategoryName { get; set; }
}
