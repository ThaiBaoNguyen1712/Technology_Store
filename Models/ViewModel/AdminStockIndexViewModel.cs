using Tech_Store.Models;

namespace Tech_Store.Models.ViewModel;

public class AdminStockIndexViewModel
{
    public IReadOnlyList<AdminStockIndexItemViewModel> Items { get; set; } = Array.Empty<AdminStockIndexItemViewModel>();

    public IReadOnlyList<Category> Categories { get; set; } = Array.Empty<Category>();

    public IReadOnlyList<Brand> Brands { get; set; } = Array.Empty<Brand>();

    public string? Sku { get; set; }

    public string? Name { get; set; }

    public string? Status { get; set; }

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public int? StockFrom { get; set; }

    public int? StockTo { get; set; }

    public int Page { get; set; }

    public int TotalPages { get; set; }

    public int TotalItems { get; set; }

    public string QueryString { get; set; } = string.Empty;
}
