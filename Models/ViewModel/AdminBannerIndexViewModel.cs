namespace Tech_Store.Models.ViewModel;

public class AdminBannerIndexViewModel
{
    public string? Keyword { get; set; }

    public int? PositionId { get; set; }

    public string? TargetType { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; } = 1;

    public int TotalPages { get; set; } = 1;

    public int TotalItems { get; set; }

    public string QueryString { get; set; } = string.Empty;

    public IReadOnlyList<BannerPositionOptionViewModel> Positions { get; set; } = [];

    public IReadOnlyList<AdminBannerIndexItemViewModel> Banners { get; set; } = [];
}
