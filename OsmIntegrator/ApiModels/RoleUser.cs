using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace OsmIntegrator.ApiModels
{
    public class RoleUser
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public Dictionary<string, bool> Roles { get; set; }
    }
}
