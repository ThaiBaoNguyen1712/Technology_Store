namespace Tech_Store.Models.ViewModel
{
    public class AdminOrderIndexViewModel
    {
        public string? Keyword { get; set; }

        public string? Status { get; set; }

        public string? PaymentStatus { get; set; }

        public DateTime? DateFrom { get; set; }

        public DateTime? DateTo { get; set; }

        public decimal? AmountFrom { get; set; }

        public decimal? AmountTo { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }

        public List<AdminOrderIndexItemViewModel> Orders { get; set; } = new();
    }
}
