using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class ForgotPassword
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
