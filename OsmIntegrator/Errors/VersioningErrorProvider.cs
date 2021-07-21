using System.Collections.Generic;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace OsmIntegrator.Errors
{
    public class ApiVersioningErrorResponseProvider : DefaultErrorResponseProvider
    {
        public override IActionResult CreateResponse(ErrorResponseContext context)
        {
            var errorResponse = new
            {
                // the list is here to keep same format as ValidationProblem
                errors = new {
                    messages = new List<string>() {context.Message}
                },
                status = (int)HttpStatusCode.BadRequest

            };
            var response = new ObjectResult(errorResponse);

            return response;
        }
    }
}

