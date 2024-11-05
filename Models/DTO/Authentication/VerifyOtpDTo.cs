namespace Tech_Store.Models.DTO.Authentication
{
    public class VerifyOtpDTo
    {
        public string Email { get; set; }
        public string OtpToken { get; set; }
        public string Action {  get; set; }
    }
}
