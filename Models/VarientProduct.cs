using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class VarientProduct
{
    public int VarientId { get; set; }

    public int? ProductId { get; set; }

    public string? Attributes { get; set; }

    public string Sku { get; set; } = null!;

    public decimal? Price { get; set; }

    public int? Stock { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Product? Product { get; set; }

    public virtual ICollection<ProductHistoryDetail> ProductHistoryDetails { get; set; } = new List<ProductHistoryDetail>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();
}
