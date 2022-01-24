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
using Microsoft.Extensions.Localization;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.ApiModels.Conversation;

namespace OsmIntegrator.Controllers
{
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
      DbMessage dbMessage = _mapper.Map<DbMessage>(message);
      dbMessage.User = user;

      await _dbContext.AddAsync(dbMessage);
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
        .Where(x => x.TileId == Guid.Parse(id) && x.StopId == null)
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
            Text = note.Messages.Aggregate("", (x, y) => x + y.Text + Environment.NewLine),
            TileId = Guid.Parse(id)
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
            Text = note.Messages.Aggregate("", (x, y) => x + y.Text + Environment.NewLine),
            TileId = Guid.Parse(id)
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

      DbMessage dbMessage = _mapper.Map<DbMessage>(approvalMessage);
      dbMessage.User = user;

      dbConversation.Messages.Add(dbMessage);

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
      DbMessage dbMessage = _mapper.Map<DbMessage>(rejectMessage);
      dbMessage.User = user;

      dbConversation.Messages.Add(dbMessage);

      dbConversation.Messages.Add(_mapper.Map<DbMessage>(rejectMessage));

      _dbContext.SaveChanges();

      return Ok(_localizer["Note rejected successfully"]);
    }
  }
}