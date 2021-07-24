using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace OsmIntegrator.Controllers
{
    [ApiController]
    public class ErrorController : ControllerBase
    {
        // this will handle handle all uncaught exceptions, along with the BadHttpRequestException which is treated as the most broad 400
        // all domain exceptions should be handled on on the controller level, in case there is nothing to do the controler should raise BadHttpRequestException

        private readonly ILogger<ErrorController> _logger;

        public ErrorController(ILogger<ErrorController> logger)
        {
            _logger = logger;
        }

        [HttpGet("/error-local-development")]
        public IActionResult ErrorLocalDevelopment(
            [FromServices] IWebHostEnvironment webHostEnvironment)
        {
            if (webHostEnvironment.EnvironmentName != "Development")
            {
                throw new InvalidOperationException(
                    "This shouldn't be invoked in non-development environments.");
            }

            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            _logger.LogError(context.Error, context.Error.Message);
            if (context.Error is BadHttpRequestException)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Bad Request",
                    status = 400,
                    // the list is here to keep same format as ValidationProblem
                    errors = new {message = new List<string>() {context.Error.Message} },
                    detail = context.Error.StackTrace


                });
            }
            return Problem(
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                detail: context.Error.StackTrace,
                title: "Internal Server Error");
        }


        [HttpGet("/error")]
        public IActionResult Error()
        {
            var context = HttpContext.Features.Get<IExceptionHandlerFeature>();
            _logger.LogError(context.Error, context.Error.Message);
            if (context.Error is BadHttpRequestException)
            {
                return BadRequest(new
                {
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                    title = "Bad Request",
                    status = 400,
                    // the list is here to keep same format as ValidationProblem
                    errors = new {message = new List<string>() {context.Error.Message} },
                });
            }
            return Problem(
                type: "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1",
                detail: context.Error.Message,
                title: "Internal Server Error"
            );
        }
    }
}
