namespace Tech_Store.Models.ViewModel
{
    public class UserVM
    {
        public int UserId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string ImageUrl { get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool IsActive { get; set; }
        public int OrderCount { get; set; }
    }
}
