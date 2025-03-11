using Tech_Store.Models.DTO.Payment.Admin;

namespace Tech_Store.Models.ViewModel
{
    public class OrderDetailVM
    {
        public int OrderId { get; set; }
        public string TotalPrice { get; set; }

        public string? DiscountPrice { get; set; }

        public string? ShippingPrice { get; set; }

        public string PaymentMethod { get; set; }

        public decimal OriginTotalPrice { get; set; }

        public string OrderStatus { get; set; }

        public Generate_User User { get; set; }

        public List<ProductVariant> ListVarientProduct { get; set; }
    }
    public class ProductVariant
    {
        public int ProductId { get; set; }
        public int VarientProductId { get; set; }
        public string ImageUrl { get; set; }
        public string Price { get; set; }
        public int Quantity { get; set; }
        public string NameProduct { get; set; }
        public string Attributes { get; set; }
        public string Slug { get; set; }
    }
    public class Generate_User
    {
        public string FullName { get; set; }
        public string PhoneNumber { get; set; }
        public string Address { get; set; }
    }

}
