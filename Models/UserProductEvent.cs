using System.ComponentModel.DataAnnotations.Schema;

namespace Tech_Store.Models;

public partial class UserProductEvent
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public string? SessionId { get; set; }

    public int ProductId { get; set; }

    public string EventType { get; set; } = null!;

    [Column(TypeName = "float")]
    public double Weight { get; set; }

    public string? Source { get; set; }

    public string? MetadataJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? User { get; set; }
}
