using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.ViewModel;

public class AdminCategoryFormViewModel
{
    public int CategoryId { get; set; }

    public int? ParentId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public string EngTitle { get; set; } = string.Empty;

    public string? Image { get; set; }

    public string? Description { get; set; }

    public int? Visible { get; set; }

    public int? VisibleOnCategoryPage { get; set; }

    public int? VisibleOnOtherPages { get; set; }

    public int SortOrder { get; set; }

    public int ReturnPage { get; set; } = 1;

    public int ReturnPageSize { get; set; } = 20;

    public string? ReturnKeyword { get; set; }
}
