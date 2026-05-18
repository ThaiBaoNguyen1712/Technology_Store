using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models;

public partial class Banner
{
    [Key]
    public int BannerId { get; set; }

    public string Name { get; set; } = null!;

    public string DesktopImageUrl { get; set; } = null!;

    public string? MobileImageUrl { get; set; }

    public string? AltText { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual BannerTarget? BannerTarget { get; set; }

    public virtual ICollection<BannerPositionMap> BannerPositionMaps { get; set; } = new List<BannerPositionMap>();
}
