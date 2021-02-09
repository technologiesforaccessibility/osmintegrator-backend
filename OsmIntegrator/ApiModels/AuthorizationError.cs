using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.Models
{
    public class AuthorizationError : Error
    {
        public AuthorizationError()
        {
            Description = "Authorization error";
        }
    }
}
