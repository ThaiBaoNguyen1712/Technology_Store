using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tech_Store.Models;

public partial class Order
{
    public int OrderId { get; set; }

    public int UserId { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OriginAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DeductAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DiscountAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? ShippingAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public int? ShippingAddressId { get; set; }

    public string? Note { get; set; }

    public DateTime? OrderDate { get; set; }

    public bool? IsReviewed { get; set; }

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Address? ShippingAddress { get; set; }

    public virtual User User { get; set; } = null!;
}
