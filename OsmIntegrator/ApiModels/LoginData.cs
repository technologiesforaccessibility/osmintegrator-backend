using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class LoginData
    {
        [Key]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}