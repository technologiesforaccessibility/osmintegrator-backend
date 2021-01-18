using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace osmintegrator.Models
{
    public class ValidationError : Error
    {
        public ValidationError()
        {
            Description = "Validation problem";
        }
    }
}
