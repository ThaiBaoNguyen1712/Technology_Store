namespace Tech_Store.Models.ViewModel
{
    public class SePayCheckoutViewModel
    {
        public int CheckoutId { get; set; }
        public int? OrderId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentContent { get; set; } = string.Empty;
        public string QrImageUrl { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = "Unpaid";
        public string OrderStatus { get; set; } = string.Empty;
        public string WebhookUrl { get; set; } = string.Empty;
    }
}
