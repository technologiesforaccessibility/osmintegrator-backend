using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class UserInformation
    {
        [Required]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
