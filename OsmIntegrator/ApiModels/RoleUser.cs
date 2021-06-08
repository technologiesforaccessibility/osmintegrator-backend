using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace OsmIntegrator.ApiModels
{
    public class RoleUser
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public List<RolePair> Roles { get; set; }
    }
}
