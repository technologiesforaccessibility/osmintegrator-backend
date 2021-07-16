using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.ApiModels.Errors;
using OsmIntegrator.Roles;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using OsmIntegrator.Validators;
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

        private readonly IModelValidator _validationHelper;

        private readonly IMapper _mapper;

        public NotesController(
            ILogger<NotesController> logger,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            IModelValidator validationHelper,
            ApplicationDbContext dbContext
        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _validationHelper = validationHelper;
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
        public async Task<ActionResult> Add([FromBody] NewNote note)
        {
            try
            {
                var validationResult = _validationHelper.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                ApplicationUser user = await _userManager.GetUserAsync(User);
                note.UserId = user.Id;

                await _dbContext.AddAsync(_mapper.Map<DbNote>(note));
                _dbContext.SaveChanges();

                return Ok("Note successfully added");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
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
        public async Task<ActionResult<List<NewNote>>> Get(string id)
        {
            try
            {
                DbTile tile = await _dbContext.Tiles.Include(x => x.Notes).
                    FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                if (tile == null)
                {
                    return BadRequest(new ValidationError() { Message = $"Tile with id {id} doesn't exist." });
                }

                ApplicationUser user = await _userManager.GetUserAsync(User);
                IList<string> roles = await _userManager.GetRolesAsync(user);

                List<DbNote> notes = tile.Notes;
                List<ExistingNote> result = new List<ExistingNote>();

                if (roles.Contains(UserRoles.SUPERVISOR) || roles.Contains(UserRoles.COORDINATOR) ||
                    roles.Contains(UserRoles.ADMIN))
                {
                    foreach (DbNote note in notes)
                    {
                        ExistingNote existingNote = _mapper.Map<ExistingNote>(note);

                        if (note.UserId == user.Id && !note.Approved)
                        {
                            existingNote.Editable = true;
                        }
                        result.Add(existingNote);
                    }
                }
                else
                {
                    foreach (DbNote note in notes)
                    {
                        ExistingNote existingNote = _mapper.Map<ExistingNote>(note);

                        if (note.UserId == user.Id && !note.Approved)
                        {
                            existingNote.Editable = true;
                            result.Add(existingNote);
                        }
                    }
                }

                return Ok(result);
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }

        [HttpPut]
        [Authorize(Roles =
            UserRoles.EDITOR + "," +
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Update([FromBody] UpdateNote note)
        {
            try
            {
                var validationResult = _validationHelper.Validate(ModelState);
                if (validationResult != null) return BadRequest(validationResult);

                DbNote dbNote = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == note.Id);

                if (dbNote == null)
                {
                    return BadRequest(new ValidationError() { Message = $"Note with id {note.Id} doesn't exist." });
                }

                dbNote.Lat = note.Lat;
                dbNote.Lon = note.Lon;
                dbNote.Text = note.Text;

                _dbContext.SaveChanges();

                return Ok("Note successfully added");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles =
            UserRoles.EDITOR + "," +
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Delete(string id)
        {
            try
            {
                ApplicationUser user = await _userManager.GetUserAsync(User);
                IList<string> roles = await _userManager.GetRolesAsync(user);

                DbNote note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                if (note == null)
                {
                    return BadRequest(new ValidationError() { Message = $"Note with id {note.Id} doesn't exist." });
                }

                // These roles can remove all notes
                if (roles.Contains(UserRoles.SUPERVISOR) || roles.Contains(UserRoles.COORDINATOR) ||
                    roles.Contains(UserRoles.ADMIN))
                {
                    _dbContext.Remove(note);
                }
                // Editor can only remove his note.
                else
                {
                    if (note.UserId == user.Id && !note.Approved)
                    {
                        _dbContext.Remove(note);
                    }
                    else
                    {
                        return BadRequest(new Error() { Message = $"Note with id: {note.Id} doesn't belong to this Editor." });
                    }
                }

                _dbContext.SaveChanges();

                return Ok("Note removed successfully!");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles =
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Approve(string id)
        {
            try
            {
                DbNote note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

                if (note == null)
                {
                    return BadRequest(new ValidationError() { Message = $"Note with id {note.Id} doesn't exist." });
                }

                if (note.Approved)
                {
                    return BadRequest(new ValidationError() { Message = $"Note with id {note.Id} already approved." });
                }

                note.Approved = true;
                _dbContext.SaveChanges();

                return Ok("Note approved successfully!");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, $"Unknown error while performing request.");
                return BadRequest(new UnknownError() { Message = e.Message });
            }
        }
    }
}