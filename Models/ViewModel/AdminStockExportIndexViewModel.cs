using Tech_Store.Models;

namespace Tech_Store.Models.ViewModel;

public class AdminStockExportIndexViewModel
{
    public IReadOnlyList<AdminStockExportVariantItemViewModel> Items { get; set; } = Array.Empty<AdminStockExportVariantItemViewModel>();

    public string? Keyword { get; set; }

    public int? CategoryId { get; set; }

    public string? Status { get; set; }

    public int? StockFrom { get; set; }

    public int? StockTo { get; set; }

    public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();

    public int Page { get; set; }

    public int TotalPages { get; set; }

    public int TotalItems { get; set; }

    public string QueryString { get; set; } = string.Empty;
}
