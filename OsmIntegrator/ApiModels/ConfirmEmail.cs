using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class ConfirmEmail
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; }

        [Required]
        [EmailAddress]
        public string OldEmail { get; set; }

        [Required]
        public string Token { get; set; }
    }
}
