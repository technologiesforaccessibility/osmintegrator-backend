using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.ApiModels.Errors
{
    public class ValidationError : Error
    {
        public ValidationError()
        {
            Description = "Validation problem";
        }
    }
}
