namespace Tech_Store.Models.ViewModel;

public class AdminCategoryIndexItemViewModel
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? EngTitle { get; set; }

    public string? Image { get; set; }

    public string? Description { get; set; }

    public int? Visible { get; set; }

    public int SortOrder { get; set; }
}
