using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Sku { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal OriginalPrice { get; set; }
    public decimal DiscountPrice { get; set; }


    public decimal? DiscountAmount { get; set; }

    public int? DiscountPercentage { get; set; }

    public int Stock { get; set; }

    public int? CategoryId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? BrandId { get; set; }

    public string? Color { get; set; }

    public string? Image { get; set; }

    public bool? Visible { get; set; }

    public string? Status { get; set; }

    public string? UrlYoutube { get; set; }
    public string? WarrantyPeriod { get; set; }
    public virtual Brand? Brand { get; set; }

    public virtual Category? Category { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<VarientProduct> VarientProducts { get; set; } = new List<VarientProduct>();

    public virtual ICollection<Gallery> Galleries { get; set; } = new List<Gallery>();
    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
}

