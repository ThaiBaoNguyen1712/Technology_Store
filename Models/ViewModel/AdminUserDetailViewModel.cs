using Tech_Store.Models.DTO;

namespace Tech_Store.Models.ViewModel
{
    public class AdminUserDetailViewModel : UserDTo
    {
        public int OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        public bool IsVerified { get; set; }

        public int ReturnPage { get; set; } = 1;

        public int ReturnPageSize { get; set; } = 20;

        public string? ReturnKeyword { get; set; }

        public string? ReturnStatus { get; set; }

        public string? ReturnEmail { get; set; }

        public string? ReturnPhoneNumber { get; set; }

        public decimal? ReturnRevenueFrom { get; set; }

        public decimal? ReturnRevenueTo { get; set; }

        public DateTime? ReturnCreatedDateFrom { get; set; }

        public DateTime? ReturnCreatedDateTo { get; set; }

        public List<AdminUserOrderSummaryItemViewModel> RecentOrders { get; set; } = new();
    }

    public class AdminUserOrderSummaryItemViewModel
    {
        public int OrderId { get; set; }

        public string OrderStatus { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public DateTime? OrderDate { get; set; }
    }
}
