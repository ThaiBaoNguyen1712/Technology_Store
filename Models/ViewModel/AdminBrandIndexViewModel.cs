namespace Tech_Store.Models.ViewModel;

public class AdminBrandIndexViewModel
{
    public string? Keyword { get; set; }

    public int? CategoryId { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; } = 1;

    public List<Category> Categories { get; set; } = new();

    public List<AdminBrandIndexItemViewModel> Brands { get; set; } = new();
}
