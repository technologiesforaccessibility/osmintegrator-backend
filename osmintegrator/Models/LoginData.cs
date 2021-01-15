﻿using System.ComponentModel.DataAnnotations;

namespace TS.Mobile.WebApp.Models
{
    public class LoginData
    {
        [Key]
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }
    }
}