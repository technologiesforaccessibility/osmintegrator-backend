using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database.Models
{
    public class ApplicationUser : IdentityUser<long>
    {
        public List<DbTile> Tiles { get; set; }

        [NotMapped]
        public IList<string> Roles { get; set; }
    }
}