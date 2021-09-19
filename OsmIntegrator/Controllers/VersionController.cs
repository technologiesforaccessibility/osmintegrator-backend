using System.Net.Mime;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace OsmIntegrator.Controllers
{
  [Produces(MediaTypeNames.Application.Json)]
  [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ApiController]
  [Route("api/[controller]")]
  [EnableCors("AllowOrigin")]
  public class VersionController : ControllerBase
  {
    public VersionController()
    {

    }

    [HttpGet()]
    public string GetVersion()
    {
      return Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
  }
}