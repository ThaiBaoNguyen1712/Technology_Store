using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.DTO
{
    public class InvoicesDTo
    {
        [Required]
        public int UserId { get; set; }

        public List<int> ListProductIds { get; set; }

        public List<int> Quantities { get; set; }

        public decimal TotalPrice { get; set; }

        public decimal? DiscountPrice { get; set; }

        public decimal? DedectPrice { get; set; }

        public string PaymentMethod { get; set; }

        public string? Voucher {  get; set; }
    }
}
