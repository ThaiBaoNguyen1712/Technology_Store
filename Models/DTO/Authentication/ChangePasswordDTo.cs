using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.DTO.Authentication
{
    public class ChangePasswordDTo
    {
        public string Email { get; set; }
        [MinLength(8)]
        public string Password { get; set; }
        [MinLength(8)]
        public string ConfirmPassword { get; set; }
        public string OldPassword { get; set; }

    }
}
