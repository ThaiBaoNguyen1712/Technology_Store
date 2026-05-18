using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models;

public partial class BannerTarget
{
    [Key]
    public int BannerTargetId { get; set; }

    public int BannerId { get; set; }

    public string TargetType { get; set; } = "url";

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public int? ProductId { get; set; }

    public string? ExternalUrl { get; set; }

    public bool OpenInNewTab { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Banner Banner { get; set; } = null!;

    public virtual Brand? Brand { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Product? Product { get; set; }
}
