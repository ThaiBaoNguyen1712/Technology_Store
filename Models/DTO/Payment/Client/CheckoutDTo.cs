namespace Tech_Store.Models.DTO.Payment.Client
{
    public class CheckoutDTo
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int VarientId { get; set; }
        public string Attributes { get; set; }
        public string ImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal SellPrice { get; set; }
        public decimal? OriginPrice { get; set; }
        public decimal? DiscountPrice { get; set; }

    }
}
