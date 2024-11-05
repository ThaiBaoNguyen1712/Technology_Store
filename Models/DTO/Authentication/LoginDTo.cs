namespace Tech_Store.Models.DTO.Authentication
{
    public class LoginDTo
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public bool Remember { get; set; }
    }
}
