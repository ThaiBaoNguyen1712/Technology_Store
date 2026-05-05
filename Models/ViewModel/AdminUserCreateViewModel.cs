using Tech_Store.Models.DTO;

namespace Tech_Store.Models.ViewModel
{
    public class AdminUserCreateViewModel : UserDTo
    {
        public int ReturnPage { get; set; } = 1;

        public int ReturnPageSize { get; set; } = 20;

        public string? ReturnStatus { get; set; }

        public string? ReturnEmail { get; set; }

        public string? ReturnPhoneNumber { get; set; }

        public decimal? ReturnRevenueFrom { get; set; }

        public decimal? ReturnRevenueTo { get; set; }

        public DateTime? ReturnCreatedDateFrom { get; set; }

        public DateTime? ReturnCreatedDateTo { get; set; }
    }
}
