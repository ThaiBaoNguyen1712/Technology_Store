namespace Tech_Store.Models.ViewModel;

public class AdminBrandIndexItemViewModel
{
    public int BrandId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Image { get; set; }

    public int SortOrder { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public List<string> CategoryNames { get; set; } = new();
}
