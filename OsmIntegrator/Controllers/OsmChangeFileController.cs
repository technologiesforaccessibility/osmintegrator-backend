using System;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Database;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Presenters;
using OsmIntegrator.Roles;
using OsmIntegrator.Validators;
using OsmIntegrator.DomainUseCases;

namespace OsmIntegrator.Controllers
{

    public class CreateChangeFileRequestInput
    {
        [Required]
        public Guid? TileUuid { get; set; }

    }

    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class OsmChangeFileController : ControllerBase
    {

        private readonly ApplicationDbContext _dbContext;
        private readonly IUseCase<CreateChangeFileInputDto> _useCase;
        private readonly ILogger<OsmChangeFileController> _logger;
        private readonly CreateChangeFileWebPresenter _presenter;
        private readonly IMapper _mapper;
        private readonly IModelValidator _modelValidator;

        public OsmChangeFileController(
            ApplicationDbContext dbContext,
            ILogger<OsmChangeFileController> logger,
            IMapper mapper,
            IUseCase<CreateChangeFileInputDto> useCase,
            CreateChangeFileWebPresenter presenter,
            IModelValidator modelValidator)
        {
            _dbContext = dbContext;
            _useCase = useCase;
            _logger = logger;
            _mapper = mapper;
            _presenter = presenter;
            _modelValidator = modelValidator;
        }

        [HttpGet]
        [Consumes(MediaTypeNames.Application.Json)]
        [Authorize(Roles = UserRoles.SUPERVISOR + "," + UserRoles.ADMIN)]
        public async Task<ActionResult> GetChangeFile([FromBody] CreateChangeFileRequestInput data)
        {
            try
            {
                var validationResult = _modelValidator.Validate(ModelState);
                if (validationResult != null)
                {
                    return BadRequest(validationResult);
                }
                AUseCaseResponse result = await _useCase.Handle(_mapper.Map<CreateChangeFileInputDto>(data));
                _presenter.Present(result);
                return File(_presenter.Content.ReadAsStream(), "text/xml", "file.xml");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Unexpected error while generating Osm change file");
                return Problem("Cannot generate osm change file");
            }

        }

    }
}