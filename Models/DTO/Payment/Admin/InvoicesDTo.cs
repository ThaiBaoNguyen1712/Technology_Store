using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.DTO.Payment.Admin
{
    public class InvoicesDTo
    {
        [Required]
        public int UserId { get; set; }

        public decimal TotalPrice { get; set; }

        public string? DiscountPrice { get; set; }

        public string? DeductPrice { get; set; }

        public string PaymentMethod { get; set; }

        public string? Voucher { get; set; }

        public decimal OriginTotalPrice { get; set; }

        public List<ProductVariant> ListVarientProduct { get; set; }
    }
    public class ProductVariant
    {
        public int VarientProductId { get; set; }
        public int Quantity { get; set; }
    }
}
