namespace Tech_Store.Models.ViewModel;

public class AdminStockProductPickerItemViewModel
{
    public int ProductId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Sku { get; set; } = string.Empty;

    public string? ProductImageUrl { get; set; }

    public string? BrandName { get; set; }

    public string? CategoryName { get; set; }

    public int Stock { get; set; }

    public int VariantCount { get; set; }
}
