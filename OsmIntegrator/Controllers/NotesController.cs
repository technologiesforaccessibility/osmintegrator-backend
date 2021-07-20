using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.Roles;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using OsmIntegrator.Validators;
using System.Transactions;
using Microsoft.AspNetCore.Cors;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;

namespace OsmIntegrator.Controllers
{
    /// <summary>
    /// This controller allows to manage user roles.
    /// Make sure you've already read this article before making any changes in the code:
    /// https://github.com/technologiesforaccessibility/osmintegrator-wiki/wiki/Permissions-and-Roles
    /// </summary>
    [ApiController]
    [EnableCors("AllowOrigin")]
    [Route("api/[controller]")]
    [Produces(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public class NotesController : ControllerBase
    {
        private readonly ApplicationDbContext _dbContext;

        private readonly ILogger<NotesController> _logger;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<ApplicationRole> _roleManager;

        private readonly IMapper _mapper;

        public NotesController(
            ILogger<NotesController> logger,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext dbContext
        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _dbContext = dbContext;
        }

        /// <summary>
        /// Add new note.
        /// </summary>
        /// <param name="note">Note object.</param>
        /// <returns>Operation satuts.</returns>
        [HttpPost]
        [Authorize(Roles =
            UserRoles.EDITOR + "," +
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Add([FromBody] Note note)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            note.UserId = user.Id;

            await _dbContext.AddAsync(_mapper.Map<DbNote>(note));
            _dbContext.SaveChanges();

            return Ok("Note successfully added");
        }

        /// <summary>
        /// It returns all notes situated at the specific tile including notes on the overlapped positions.
        /// </summary>
        /// <param name="id">Tile id</param>
        /// <returns>List with notes.</returns>
        [HttpGet("{id}")]
        [Authorize(Roles =
            UserRoles.EDITOR + "," +
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult<List<Note>>> Get(string id)
        {
            DbTile tile = await _dbContext.Tiles.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if(tile == null)
            {
                throw new BadHttpRequestException($"Tile with id {id} doesn't exist.");
            }

            List<DbNote> notes = await _dbContext.Notes.Where(x =>
                x.Lat >= tile.OverlapMinLat &&
                x.Lat < tile.OverlapMaxLat &&
                x.Lon >= tile.OverlapMinLon &&
                x.Lon < tile.OverlapMaxLon).ToListAsync();

            return Ok(_mapper.Map<List<Note>>(notes));
        }
    }
}