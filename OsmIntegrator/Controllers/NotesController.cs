using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
using OsmIntegrator.ApiModels;
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

      DbConversation dbConversation = _mapper.Map<DbConversation>(new Conversation()
      {
        Lat = note.Lat,
        Lon = note.Lon,
        TileId = (Guid)note.TileId,
        Id = new Guid(),
      });
      await _dbContext.AddAsync(dbConversation);

      Message message = new Message()
      {
        UserId = user.Id,
        Status = NoteStatus.Created,
        Text = note.Text,
        CreatedAt = DateTime.Now,
        ConversationId = dbConversation.Id
      };

      await _dbContext.AddAsync(_mapper.Map<DbMessage>(message));
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
      DbTile tile = await _dbContext.Tiles.
          FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

      if (tile == null)
      {
        throw new BadHttpRequestException(_localizer["Tile doesn't exist"]);
      }
      ApplicationUser user = await _userManager.GetUserAsync(User);
      IList<string> roles = await _userManager.GetRolesAsync(user);

      List<DbConversation> notes = await _dbContext.Conversations
        .Where(x => x.Id == Guid.Parse(id) && x.StopId == null)
        .Include(x => x.Messages)
        .ToListAsync();
      List<ExistingNote> result = new List<ExistingNote>();

      if (roles.Contains(UserRoles.SUPERVISOR) || roles.Contains(UserRoles.COORDINATOR) ||
          roles.Contains(UserRoles.ADMIN))
      {
        foreach (DbConversation note in notes)
        {
          ExistingNote existingNote = new ExistingNote()
          {
            Id = note.Id,
            Lat = (double)note.Lat,
            Lon = (double)note.Lon,
            Text = note.Messages.Aggregate("", (x, y) => x + y.Text + Environment.NewLine)
          };
          DbMessage message = note.Messages.LastOrDefault();

          if (message == null)
          {
            continue;
          }

          existingNote.Status = message.Status;
          existingNote.UserId = message.UserId;


          if (message.UserId == user.Id && message.Status != NoteStatus.Approved &&
              message.Status != NoteStatus.Rejected)
          {
            existingNote.Editable = true;
          }
          result.Add(existingNote);
        }
      }
      else
      {
        foreach (DbConversation note in notes)
        {
          ExistingNote existingNote = new ExistingNote()
          {
            Id = note.Id,
            Lat = (double)note.Lat,
            Lon = (double)note.Lon,
            Text = note.Messages.Aggregate("", (x, y) => x + y.Text + Environment.NewLine)
          };
          DbMessage message = note.Messages.LastOrDefault();

          if (message == null)
          {
            continue;
          }

          existingNote.Status = message.Status;
          existingNote.UserId = message.UserId;

          if (message.UserId == user.Id && message.Status != NoteStatus.Approved &&
              message.Status != NoteStatus.Rejected)
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

    /// <summary>
    /// Deprecated
    /// </summary>
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
      DbConversation dbConversation = await _dbContext.Conversations.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

      ApplicationUser user = await _userManager.GetUserAsync(User);

      if (dbConversation == null)
      {
        throw new BadHttpRequestException(_localizer["Selected note doesn't exist"]);
      }

      Message approvalMessage = new Message()
      {
        Id = new Guid(),
        UserId = user.Id,
        ConversationId = dbConversation.Id,
        Status = NoteStatus.Approved,
        CreatedAt = DateTime.Now
      };

      dbConversation.Messages.Add(_mapper.Map<DbMessage>(approvalMessage));

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
      DbConversation dbConversation = await _dbContext.Conversations.FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

      ApplicationUser user = await _userManager.GetUserAsync(User);

      if (dbConversation == null)
      {
        throw new BadHttpRequestException(_localizer["Selected note doesn't exist"]);
      }

      Message rejectMessage = new Message()
      {
        Id = new Guid(),
        UserId = user.Id,
        ConversationId = dbConversation.Id,
        Status = NoteStatus.Rejected,
        CreatedAt = DateTime.Now
      };

      dbConversation.Messages.Add(_mapper.Map<DbMessage>(rejectMessage));

      _dbContext.SaveChanges();

      return Ok(_localizer["Note rejected successfully"]);
    }
  }
}