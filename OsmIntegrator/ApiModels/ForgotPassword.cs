using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.Models
{
    public class ForgotPassword
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
