using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class ResetEmail
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
