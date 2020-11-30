using System.ComponentModel.DataAnnotations;

namespace osmintegrator.Models
{
    public class RegisterData
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "PASSWORD_MIN_LENGTH", MinimumLength = 6)]
        public string Password { get; set; }
    }
}
