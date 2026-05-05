namespace Tech_Store.Models.ViewModel;

public class AdminCategoryIndexViewModel
{
    public string? Keyword { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; } = 1;

    public List<AdminCategoryIndexItemViewModel> Categories { get; set; } = new();
}
