namespace Tech_Store.Models.ViewModel
{
    public class AdminUserIndexViewModel
    {
        public List<UserVM> Users { get; set; } = new();

        public string? Keyword { get; set; }

        public string? Status { get; set; }

        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }

        public decimal? RevenueFrom { get; set; }

        public decimal? RevenueTo { get; set; }

        public DateTime? CreatedDateFrom { get; set; }

        public DateTime? CreatedDateTo { get; set; }

        public int Page { get; set; } = 1;

        public int PageSize { get; set; } = 20;

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }
    }
}
