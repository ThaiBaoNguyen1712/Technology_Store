using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public class ProductDTo
{
    public int ProductId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? Sku { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? OriginalPrice { get; set; }

    public decimal? SellPrice { get; set; }

    public decimal? DiscountAmount { get; set; }

    public int? DiscountPercentage { get; set; }

    public int Stock { get; set; }

    public int? CategoryId { get; set; }

    public int? BrandId { get; set; }

    public string? Color { get; set; }

    public IFormFile? Image { get; set; }

    public bool? Visible { get; set; }

    public string? Status { get; set; }

    public string? UrlYoutube { get; set; }

    public string? WarrantyPeriod { get; set; }

    public double? Weight { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<IFormFile>? Galleries { get; set; } = new List<IFormFile>();

    public virtual ICollection<VarientProduct>? VarientProducts { get; set; } = new List<VarientProduct>();
}
