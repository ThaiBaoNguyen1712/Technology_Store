using System;
using System.Collections.Generic;
using Tech_Store.Models.DTO;

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

    public int SortOrder { get; set; }

    public string? UrlYoutube { get; set; }

    public string? WarrantyPeriod { get; set; }

    public double? Weight { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    
    public virtual ICollection<IFormFile>? Galleries { get; set; } = new List<IFormFile>();

    //Product Variant
    public bool? IsUseVariant { get; set; }

    public virtual ICollection<VarientProductDTo>? VarientProducts { get; set; } = new List<VarientProductDTo>();

    public virtual ICollection<ProductSpecValueDTo>? SpecValues { get; set; } = new List<ProductSpecValueDTo>();

    public int ReturnPage { get; set; } = 1;

    public int ReturnPageSize { get; set; } = 25;

    public string? ReturnStatus { get; set; }

    public string? ReturnSkuKeyword { get; set; }

    public string? ReturnNameKeyword { get; set; }

    public int? ReturnCategoryId { get; set; }

    public int? ReturnBrandId { get; set; }

    public int? ReturnStockFrom { get; set; }

    public int? ReturnStockTo { get; set; }

}
