namespace Tech_Store.Models.ViewModel
{
    public class AdminTransactionIndexViewModel
    {
        public List<AdminTransactionListItemViewModel> Transactions { get; set; } = new();

        public int? TransactionId { get; set; }

        public int? OrderId { get; set; }

        public string? CustomerName { get; set; }

        public string? PhoneNumber { get; set; }

        public string? PaymentStatus { get; set; }

        public string? PaymentMethod { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public decimal? AmountFrom { get; set; }

        public decimal? AmountTo { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }
    }
}
