using System.ComponentModel.DataAnnotations.Schema;

namespace Tech_Store.Models;

public partial class PendingSePayCheckout
{
    public int PendingSePayCheckoutId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "decimal(20,2)")]
    public decimal Amount { get; set; }

    public string PaymentContent { get; set; } = null!;

    public string CheckoutPayload { get; set; } = null!;

    public string PaymentStatus { get; set; } = null!;

    public string? OrderStatus { get; set; }

    public int? OrderId { get; set; }

    public DateTime? PaidAt { get; set; }

    public string? GatewayPayload { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}
