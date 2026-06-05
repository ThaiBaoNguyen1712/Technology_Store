namespace Tech_Store.Models;

public partial class UserProductEvent
{
    public long Id { get; set; }

    public int? UserId { get; set; }

    public string? SessionId { get; set; }

    public int ProductId { get; set; }

    public int ViewCount { get; set; }

    public int AddToCartCount { get; set; }

    public int WishlistCount { get; set; }

    public int PurchaseCount { get; set; }

    public double InteractionScore { get; set; }

    public DateTime LastInteractedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User? User { get; set; }
}
