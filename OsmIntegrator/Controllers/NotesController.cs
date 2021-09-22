using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OsmIntegrator.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using Microsoft.AspNetCore.Cors;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using Microsoft.Extensions.Localization;

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
        private readonly IStringLocalizer<NotesController> _localizer;

        public NotesController(
            ILogger<NotesController> logger,
            IMapper mapper,
            UserManager<ApplicationUser> userManager,
            RoleManager<ApplicationRole> roleManager,
            ApplicationDbContext dbContext,
            IStringLocalizer<NotesController> localizer

        )
        {
            _logger = logger;
            _userManager = userManager;
            _mapper = mapper;
            _roleManager = roleManager;
            _dbContext = dbContext;
            _localizer = localizer;
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
            ApplicationUser user = await _userManager.GetUserAsync(User);
            note.UserId = user.Id;

            DbNote newNote = _mapper.Map<DbNote>(note);
            newNote.Status = NoteStatus.Created;

            await _dbContext.AddAsync(newNote);
            _dbContext.SaveChanges();

            return Ok(_localizer["Note successfully added"]);
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
            DbTile tile = await _dbContext.Tiles.Include(x => x.Notes).
                FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (tile == null)
            {
                throw new BadHttpRequestException(_localizer["Tile doesn't exist"]);
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

                    if (note.UserId == user.Id && note.Status != NoteStatus.Approved && 
                        note.Status != NoteStatus.Rejected)
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

                    if (note.UserId == user.Id && note.Status != NoteStatus.Approved && 
                        note.Status != NoteStatus.Rejected)
                    {
                        existingNote.Editable = true;
                        result.Add(existingNote);
                    }
                }
            }

            return Ok(result);
        }

        [HttpPut]
        [Authorize(Roles =
            UserRoles.EDITOR + "," +
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Update([FromBody] UpdateNote note)
        {
            DbNote dbNote = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == note.Id);

            if (dbNote == null)
            {
                throw new BadHttpRequestException(_localizer["Selected note doesn't exist"]);
            }

            dbNote.Lat = note.Lat;
            dbNote.Lon = note.Lon;
            dbNote.Text = note.Text;

            _dbContext.SaveChanges();

            return Ok(_localizer["Note successfully updated"]);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles =
            UserRoles.EDITOR + "," +
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Delete(string id)
        {
            ApplicationUser user = await _userManager.GetUserAsync(User);
            IList<string> roles = await _userManager.GetRolesAsync(user);

            DbNote note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (note == null)
            {
                throw new BadHttpRequestException(_localizer["Selected note doesn't exist"]);
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
                if (note.UserId == user.Id && note.Status != NoteStatus.Approved && 
                    note.Status != NoteStatus.Rejected)
                {
                    _dbContext.Remove(note);
                }
                else
                {
                    throw new BadHttpRequestException(_localizer["Note doesn't belong to editor"]);
                }
            }

            _dbContext.SaveChanges();

            return Ok(_localizer["Note removed successfully"]);
        }

        [HttpPut("Approve/{id}")]
        [Authorize(Roles =
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Approve(string id)
        {
            DbNote note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (note == null)
            {
                throw new BadHttpRequestException(_localizer["Selected note doesn't exist"]);
            }

            if (note.Status == NoteStatus.Approved)
            {
                throw new BadHttpRequestException(_localizer["Note already approved"]);
            }

            note.Status = NoteStatus.Approved;
            _dbContext.SaveChanges();

            return Ok(_localizer["Note approved successfully"]);
        }

        [HttpPut("Reject/{id}")]
        [Authorize(Roles =
            UserRoles.SUPERVISOR + "," +
            UserRoles.COORDINATOR + "," +
            UserRoles.ADMIN)]
        public async Task<ActionResult> Reject(string id)
        {
            DbNote note = await _dbContext.Notes.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

            if (note == null)
            {
                throw new BadHttpRequestException(_localizer["Selected note doesn't exist"]);
            }

            if (note.Status == NoteStatus.Rejected)
            {
                throw new BadHttpRequestException(_localizer["Note already rejected"]);
            }

            note.Status = NoteStatus.Rejected;
            _dbContext.SaveChanges();

            return Ok(_localizer["Note rejected successfully"]);
        }
    }
}