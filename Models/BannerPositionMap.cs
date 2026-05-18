using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models;

public partial class BannerPositionMap
{
    [Key]
    public int BannerPositionMapId { get; set; }

    public int BannerId { get; set; }

    public int BannerPositionId { get; set; }

    public int? DisplayCategoryId { get; set; }

    public int? DisplayBrandId { get; set; }

    public int Priority { get; set; }

    public DateTime? StartAt { get; set; }

    public DateTime? EndAt { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Banner Banner { get; set; } = null!;

    public virtual BannerPosition BannerPosition { get; set; } = null!;

    public virtual Category? DisplayCategory { get; set; }

    public virtual Brand? DisplayBrand { get; set; }
}
