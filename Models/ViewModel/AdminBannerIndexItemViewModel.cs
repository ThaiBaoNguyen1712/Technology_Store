namespace Tech_Store.Models.ViewModel;

public class AdminBannerIndexItemViewModel
{
    public int BannerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DesktopImageUrl { get; set; } = string.Empty;

    public string TargetSummary { get; set; } = string.Empty;

    public string PositionSummary { get; set; } = string.Empty;

    public string ScheduleSummary { get; set; } = string.Empty;

    public int HighestPriority { get; set; }

    public bool IsActive { get; set; }

    public bool HasDefaultPosition { get; set; }
}
