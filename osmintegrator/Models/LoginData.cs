using System.ComponentModel.DataAnnotations;

namespace TS.Mobile.WebApp.Models
{
    public class LoginData
    {
        [Key]
        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}