using System;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database.Models
{
    public class ApplicationUserRole : IdentityUserRole<Guid>
    {
        public ApplicationUser User { get; set; }

        public ApplicationRole Role { get; set; }
    }
}