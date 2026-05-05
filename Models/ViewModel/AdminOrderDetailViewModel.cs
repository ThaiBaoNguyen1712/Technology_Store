namespace Tech_Store.Models.ViewModel
{
    public class AdminOrderDetailViewModel
    {
        public int OrderId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = "-";

        public string? Note { get; set; }

        public DateTime? OrderDate { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string OrderStatus { get; set; } = string.Empty;

        public string OrderStatusLabel { get; set; } = string.Empty;

        public string OrderStatusBadgeClass { get; set; } = string.Empty;

        public string PaymentStatus { get; set; } = string.Empty;

        public string PaymentStatusLabel { get; set; } = string.Empty;

        public string PaymentStatusBadgeClass { get; set; } = string.Empty;

        public string PaymentMethod { get; set; } = "-";

        public DateTime? PaymentDate { get; set; }

        public int ItemCount { get; set; }

        public decimal OriginAmount { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal DeductAmount { get; set; }

        public decimal ShippingAmount { get; set; }

        public decimal TotalAmount { get; set; }

        public List<AdminOrderDetailItemViewModel> Items { get; set; } = new();
    }
}
