using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.DTO
{
    public class UserDTo
    {
       
        public int UserId { get; set; }
        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        [Phone] // Kiểm tra định dạng số điện thoại
        public string PhoneNumber { get; set; }

        [Required]
        [EmailAddress] // Kiểm tra định dạng email
        public string Email { get; set; }

        public IFormFile? Image { get; set; }
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; }

        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? Province { get; set; }
        public string? AddressLine { get; set; }
    }
}
