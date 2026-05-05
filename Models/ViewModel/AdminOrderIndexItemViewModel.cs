namespace Tech_Store.Models.ViewModel
{
    public class AdminOrderIndexItemViewModel
    {
        public int OrderId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public DateTime? OrderDate { get; set; }

        public string OrderStatus { get; set; } = string.Empty;

        public string OrderStatusLabel { get; set; } = string.Empty;

        public string OrderStatusBadgeClass { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string PaymentStatusLabel { get; set; } = string.Empty;

        public string PaymentStatusBadgeClass { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = "-";

        public decimal TotalAmount { get; set; }

        public int ItemCount { get; set; }

        public string ProductSummary { get; set; } = string.Empty;
    }
}
