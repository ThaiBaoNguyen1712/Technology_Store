using Tech_Store.Models.ViewModel;

namespace Tech_Store.Models.DTO
{
    public class AdminProductIndexData
    {
        public List<Product> Products { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public AdminProductFilterRequest Filters { get; set; } = new();
        public int Page { get; set; } = 1;
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; } = 1;
    }

    public class AdminProductLookupData
    {
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<Models.Attribute> Attributes { get; set; } = new();
        public List<Specs> Specs { get; set; } = new();
    }

    public class AdminProductDetailData
    {
        public Product? Product { get; set; }
        public decimal? TotalSell { get; set; }
        public int ReviewCount { get; set; }
        public int OrderCount { get; set; }
    }

    public class AdminProductEditData : AdminProductLookupData
    {
        public Product? Product { get; set; }
        public List<int> CheckedAttributeIds { get; set; } = new();
        public List<ProductSpecValueDTo> ProductSpecValues { get; set; } = new();
    }

    public class AdminProductCodeData
    {
        public Product? Product { get; set; }
        public string? Content { get; set; }
        public string? CodeType { get; set; }
        public string? QRCodeImage { get; set; }
        public string? BarcodeImage { get; set; }
    }

    public class AdminProductActionResult
    {
        public bool Success { get; set; }
        public bool NotFound { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool? Visible { get; set; }
        public string? Html { get; set; }
    }

    public class AdminProductFilterRequest
    {
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? Status { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public int? StockFrom { get; set; }
        public int? StockTo { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
    }

    public class ProductSpecValueDTo
    {
        public int? SpecValueId { get; set; }
        public int SpecId { get; set; }
        public string SpecName { get; set; } = string.Empty;
        public string SpecCode { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public string? Unit { get; set; }
        public string InputType { get; set; } = "text";
        public int SortOrder { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    public class SpecDefinitionDTo
    {
        public int? SpecId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? GroupName { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
        public string InputType { get; set; } = "text";
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsFilterable { get; set; }
        public bool IsVisibleOnProductPage { get; set; } = true;
    }
}
