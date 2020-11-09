using System.ComponentModel.DataAnnotations;

namespace BookStore_API.DTOs
{
    public class UserDTO
    {
        [Required]
        [EmailAddress]
        public string EmailAddress { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(15, ErrorMessage = "Your Password is limited to {2} to {1} characters ", MinimumLength = 3)]
        public string Password { get; set; }

    }
}