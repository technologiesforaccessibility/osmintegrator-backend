using System.Collections.Generic;
using OsmIntegrator.Database.Models;

namespace OsmIntegrator.Extensions
{
    public static class ApplicationUserExtension
    {
        public static List<string> GetRoles(this ApplicationUser user)
        {
            List<string> result = new List<string>();

            if(user.UserRoles != null)
            {
                user.UserRoles.ForEach(x => result.Add(x.Role.Name));
            }

            return result;
        }
    }
}