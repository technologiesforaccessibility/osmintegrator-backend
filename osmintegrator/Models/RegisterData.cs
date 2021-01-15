using System.ComponentModel.DataAnnotations;

namespace osmintegrator.Models
{
    public class RegisterData
    {
        [Required]
        [EmailAddress(ErrorMessage ="User name must be an email")]
        public string Email { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "PASSWORD_MIN_LENGTH", MinimumLength = 6)]
        public string Password { get; set; }
    }
}
