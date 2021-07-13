using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database.Models
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public List<DbTile> Tiles { get; set; }

        [NotMapped]
        public IList<string> Roles { get; set; }
        
        public List<DbTile> ApprovedTiles { get; set; }
    }
}