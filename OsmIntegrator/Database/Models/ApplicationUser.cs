using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public List<DbTile> Tiles { get; set; }

        public List<ApplicationUserRole> UserRoles { get; set; }
    }
}