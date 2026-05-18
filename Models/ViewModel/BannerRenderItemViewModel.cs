namespace Tech_Store.Models.ViewModel;

public class BannerRenderItemViewModel
{
    public int BannerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string DesktopImageUrl { get; set; } = string.Empty;

    public string? MobileImageUrl { get; set; }

    public string AltText { get; set; } = string.Empty;

    public string NavigateUrl { get; set; } = "/";

    public bool OpenInNewTab { get; set; }

    public int Priority { get; set; }
}
