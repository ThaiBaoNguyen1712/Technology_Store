namespace Tech_Store.Models.DTO.Payment.Client
{
    public class PaymentDTo
    {
        public int UserId { get; set; } = int.MinValue;
        public string? VoucherCode { get; set; } = string.Empty;
        public string? Note { get; set; } = string.Empty;
        public decimal? ShipAmount { get; set; } = decimal.MinValue;
        public bool NewAddress { get; set; } = false;
        public Address? Address { get; set; }
        public List<VarientProduct> Products { get; set; }
    }
    public class VarientProduct
    {
        public int VarientProductID { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
    public class ShipAddress
    {
        public string AddressLine { get; set; }
        public int Ward { get; set; }
        public int District { get; set; }
        public int Province { get; set; }
    }
}
