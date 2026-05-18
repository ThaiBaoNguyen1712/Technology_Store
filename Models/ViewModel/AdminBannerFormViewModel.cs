using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Tech_Store.Models.ViewModel;

public class AdminBannerFormViewModel
{
    public int BannerId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên banner.")]
    public string Name { get; set; } = string.Empty;

    public string? DesktopImageUrl { get; set; }

    public string? MobileImageUrl { get; set; }

    public IFormFile? DesktopImageFile { get; set; }

    public IFormFile? MobileImageFile { get; set; }

    public string? AltText { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn loại điều hướng.")]
    public string TargetType { get; set; } = "url";

    public string? ExternalUrl { get; set; }

    public int? TargetCategoryId { get; set; }

    public int? TargetBrandId { get; set; }

    public int? TargetProductId { get; set; }

    public bool OpenInNewTab { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Notes { get; set; }

    public List<BannerPositionMapInputViewModel> PositionMaps { get; set; } = [];

    public IReadOnlyList<BannerLookupOptionViewModel> Categories { get; set; } = [];

    public IReadOnlyList<BannerLookupOptionViewModel> Brands { get; set; } = [];

    public IReadOnlyList<BannerPositionOptionViewModel> Positions { get; set; } = [];

    public IReadOnlyList<BannerLookupOptionViewModel> InitialProducts { get; set; } = [];

    public int ReturnPage { get; set; } = 1;

    public string? ReturnKeyword { get; set; }

    public int? ReturnPositionId { get; set; }

    public string? ReturnTargetType { get; set; }

    public string? ReturnStatus { get; set; }
}
