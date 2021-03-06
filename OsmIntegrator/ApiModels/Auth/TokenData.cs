﻿using OsmIntegrator.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels.Auth
{
    public class TokenData
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}
