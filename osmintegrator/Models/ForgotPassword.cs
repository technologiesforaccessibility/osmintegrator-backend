using System.ComponentModel.DataAnnotations;

namespace osmintegrator.Models
{
    public class ForgotPassword
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
