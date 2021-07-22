using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels.Auth
{
    public class RegisterData
    {
        [Required]
        [EmailAddress(ErrorMessage ="User name must be an email")]
        public string? Email { get; set; }

        [Required]
        [StringLength(16, ErrorMessage = "Username must be at least 5 and not longer than 16 characters")]
        public string? Username { get; set; }

        [Required]
        [StringLength(32, ErrorMessage = "Password length must me at least 8 and not longer than 32 characters", MinimumLength = 8)]
        public string? Password { get; set; }
    }
}
