using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.ViewModel;

public class AdminBrandFormViewModel
{
    public int BrandId { get; set; }

    [Required(ErrorMessage = "Tên thương hiệu là bắt buộc.")]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? ExistingImage { get; set; }

    [Display(Name = "Thứ tự hiển thị")]
    public int SortOrder { get; set; }

    public int ReturnPage { get; set; } = 1;

    public int ReturnPageSize { get; set; } = 20;

    public string? ReturnKeyword { get; set; }

    public int? ReturnCategoryId { get; set; }

    public List<int> CategoryIds { get; set; } = new();
}
