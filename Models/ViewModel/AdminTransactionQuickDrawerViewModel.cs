namespace Tech_Store.Models.ViewModel
{
    public class AdminTransactionQuickDrawerViewModel
    {
        public int PaymentId { get; set; }

        public int OrderId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = string.Empty;

        public string PaymentMethodLabel { get; set; } = string.Empty;

        public string PaymentMethodAsset { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string PaymentStatusLabel { get; set; } = string.Empty;

        public string OrderStatus { get; set; } = string.Empty;

        public decimal Amount { get; set; }

        public DateTime? PaymentDate { get; set; }

        public DateTime? CreatedAt { get; set; }

        public string OrderDetailUrl { get; set; } = "/Admin/Orders";
    }
}
