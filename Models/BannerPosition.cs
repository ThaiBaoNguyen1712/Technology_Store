using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models;

public partial class BannerPosition
{
    [Key]
    public int BannerPositionId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BannerPositionMap> BannerPositionMaps { get; set; } = new List<BannerPositionMap>();
}
