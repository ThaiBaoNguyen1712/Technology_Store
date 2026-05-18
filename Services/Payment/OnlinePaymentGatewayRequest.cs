namespace Tech_Store.Services.Payment
{
    public class OnlinePaymentGatewayRequest
    {
        public decimal Amount { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }

        public int OrderId { get; set; }
    }
}
