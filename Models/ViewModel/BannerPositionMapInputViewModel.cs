using Tech_Store.Models.Enums;

namespace Tech_Store.Models.ViewModel;

public class BannerPositionMapInputViewModel
{
    public int? BannerPositionMapId { get; set; }

    public int BannerPositionId { get; set; }

    public int? DisplayCategoryId { get; set; }

    public int? DisplayBrandId { get; set; }

    public BannerScopeType ScopeType { get; set; } = BannerScopeType.All;

    public int Priority { get; set; } = 100;

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;
}
