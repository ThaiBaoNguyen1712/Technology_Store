namespace Tech_Store.Models.ViewModel;

public class AdminStockProductPickerViewModel
{
    public IReadOnlyList<AdminStockProductPickerItemViewModel> Items { get; set; } = Array.Empty<AdminStockProductPickerItemViewModel>();

    public string? Keyword { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }

    public int TotalPages { get; set; }

    public int TotalItems { get; set; }
}
