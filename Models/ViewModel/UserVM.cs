namespace Tech_Store.Models.ViewModel
{
    public class UserVM
    {
        public int UserId { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public string LastLoginIp { get; set; } = string.Empty;
        public string LastLoginDevice { get; set; } = string.Empty;
        public DateTime? LastRequestAt { get; set; }
        public string LastRequestIp { get; set; } = string.Empty;
    }
}
