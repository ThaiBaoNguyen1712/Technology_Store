using Tech_Store.Models.ViewModel;
using X.PagedList;

namespace Tech_Store.Models.DTO
{
    public class AdminProductIndexData
    {
        public IPagedList<Product> Products { get; set; } = new StaticPagedList<Product>(new List<Product>(), 1, 1, 0);
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public AdminProductFilterRequest Filters { get; set; } = new();
        public int PageSize { get; set; }
    }

    public class AdminProductLookupData
    {
        public List<Category> Categories { get; set; } = new();
        public List<Brand> Brands { get; set; } = new();
        public List<Models.Attribute> Attributes { get; set; } = new();
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
}
