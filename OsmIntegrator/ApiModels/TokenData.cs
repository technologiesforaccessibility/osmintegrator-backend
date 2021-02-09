using OsmIntegrator.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.Models
{
    public class TokenData
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}
