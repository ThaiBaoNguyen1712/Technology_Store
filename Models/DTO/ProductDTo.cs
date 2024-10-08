namespace Tech_Store.Models.DTO
{
    public class ProductDTo
    {
        public int ProductId { get; set; }

        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string? Modal { get; set; }

        public decimal OriginalPrice { get; set; }

        public decimal? DiscountedPrice { get; set; }

        public int? DiscountPercentage { get; set; }

        public bool? IsDiscounted { get; set; }

        public int Stock { get; set; }

        public int? CategoryId { get; set; }

        public int? BrandId { get; set; }

        public string? Color { get; set; }

        public string? Image { get; set; }

        public bool? Visible { get; set; }

        public string? Status { get; set; }

        public string? WarrantyPeriod { get; set; }

        public virtual Brand? Brand { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public ICollection<IFormFile> Gallery { get; set; }
    }
}
