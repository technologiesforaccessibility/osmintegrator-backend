using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels.Auth
{
    public class ForgotPassword
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
