using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System;

namespace OsmIntegrator.ApiModels
{
    public class User
    {
        [Required]
        public Guid? Id { get; set; }
        public string UserName { get; set; }

        [EmailAddress]
        public string Email { get; set; }

        public List<string> Roles { get; set; }
    }
}
