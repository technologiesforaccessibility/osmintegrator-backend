using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.ApiModels
{
    public class UnknownError : Error
    {
        public UnknownError()
        {
            Description = "Unknown error";
        }
    }
}
