using System.Collections.Generic;
using System.Net;

namespace OsmIntegrator.Interfaces {
    public abstract class AUseCaseResponse
    {        
        public string Message { get; }        
        public IEnumerable<string> Errors {  get; }

        protected AUseCaseResponse(string message, IEnumerable<string> errors = null)
        {   
            Errors = errors;                    
            Message = message;
        }
    }
}