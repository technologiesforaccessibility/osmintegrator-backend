using osmintegrator.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace osmintegrator.Models
{
    public class TokenData
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}
