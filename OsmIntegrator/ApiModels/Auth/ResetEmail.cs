using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels.Auth
{
    public class ResetEmail
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }
    }
}
