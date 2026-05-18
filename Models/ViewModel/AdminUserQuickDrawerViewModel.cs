namespace Tech_Store.Models.ViewModel
{
    public class AdminUserQuickDrawerViewModel
    {
        public int UserId { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Email { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; }

        public bool IsVerified { get; set; }

        public int OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? LastLogin { get; set; }

        public string? LastLoginIp { get; set; }

        public string? LastLoginDevice { get; set; }

        public DateTime? LastRequestAt { get; set; }

        public string? LastRequestIp { get; set; }

        public string? LastRequestDevice { get; set; }

        public string? AddressLine { get; set; }

        public string? Ward { get; set; }

        public string? District { get; set; }

        public string? Province { get; set; }

        public string? Address { get; set; }

        public string DetailUrl { get; set; } = string.Empty;
    }
}
