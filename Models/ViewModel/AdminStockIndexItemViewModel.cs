namespace Tech_Store.Models.ViewModel;

public class AdminStockIndexItemViewModel
{
    public int ProductId { get; set; }

    public string? ImageUrl { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Sku { get; set; }

    public string? BrandName { get; set; }

    public string? CategoryName { get; set; }

    public decimal? SellPrice { get; set; }

    public int Stock { get; set; }

    public string? Status { get; set; }
}
