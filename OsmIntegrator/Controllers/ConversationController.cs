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
  public class ConversationController : ControllerBase
  {
    private readonly ApplicationDbContext _dbContext;

    private readonly ILogger<ConversationController> _logger;

    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<ApplicationRole> _roleManager;

    private readonly IMapper _mapper;
    private readonly IStringLocalizer<ConversationController> _localizer;

    public ConversationController(
        ILogger<ConversationController> logger,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext dbContext,
        IStringLocalizer<ConversationController> localizer

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
    /// Add new message.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Operation satuts.</returns>
    [HttpPost]
    [Authorize(Roles =
        UserRoles.EDITOR + "," +
        UserRoles.SUPERVISOR + "," +
        UserRoles.COORDINATOR + "," +
        UserRoles.ADMIN)]
    public async Task<ActionResult> AddMessage([FromBody] Message message)
    {
      ApplicationUser user = await _userManager.GetUserAsync(User);
      message.UserId = user.Id;

      DbMessage dbMessage = _mapper.Map<DbMessage>(message);
      dbMessage.Status = NoteStatus.Created;

      if (message.ConversationId == null)
      {
        DbConversation dbConversation = _mapper.Map<DbConversation>(message.Conversation);
        await _dbContext.AddAsync(dbConversation);

        dbMessage.Conversation = dbConversation;
      }

      await _dbContext.AddAsync(dbMessage);
      _dbContext.SaveChanges();

      return Ok(_localizer["Message successfully added"]);
    }

    /// <summary>
    /// It returns all conversations situated at the specific tile including conversations on the overlapped positions.
    /// </summary>
    /// <param name="id">Tile id</param>
    /// <returns>List with conversations.</returns>
    [HttpGet("{id}")]
    [Authorize(Roles =
        UserRoles.EDITOR + "," +
        UserRoles.SUPERVISOR + "," +
        UserRoles.COORDINATOR + "," +
        UserRoles.ADMIN)]
    public async Task<ActionResult<List<Conversation>>> Get(string id)
    {
      DbTile tile = await _dbContext.Tiles.
          FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

      if (tile == null)
      {
        throw new BadHttpRequestException(_localizer["Tile doesn't exist"]);
      }
      ApplicationUser user = await _userManager.GetUserAsync(User);

      List<DbConversation> dbConversations = await _dbContext.Conversations
        .Include(x => x.Messages)
        .Where(x => x.TileId == tile.Id)
        .ToListAsync();

      List<Conversation> conversations = _mapper.Map<List<Conversation>>(dbConversations);


      return Ok(conversations);
    }


    [HttpPut("Approve/{id}")]
    [Authorize(Roles =
        UserRoles.SUPERVISOR + "," +
        UserRoles.COORDINATOR + "," +
        UserRoles.ADMIN)]
    public async Task<ActionResult> Approve(string id)
    {
      DbConversation dbConversation = await _dbContext.Conversations
        .Include(x => x.Messages)
        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

      ApplicationUser user = await _userManager.GetUserAsync(User);

      if (dbConversation == null)
      {
        throw new BadHttpRequestException(_localizer["Selected conversation doesn't exist"]);
      }

      Message approvalMessage = new Message()
      {
        Id = new Guid(),
        UserId = user.Id,
        ConversationId = dbConversation.Id,
        Status = NoteStatus.Approved,
      };

      dbConversation.Messages.Add(_mapper.Map<DbMessage>(approvalMessage));
      _dbContext.SaveChanges();

      return Ok(_localizer["Conversation approved successfully"]);
    }

    [HttpPut("Reject/{id}")]
    [Authorize(Roles =
        UserRoles.SUPERVISOR + "," +
        UserRoles.COORDINATOR + "," +
        UserRoles.ADMIN)]
    public async Task<ActionResult> Reject(string id)
    {
      DbConversation dbConversation = await _dbContext.Conversations
        .Include(x => x.Messages)
        .FirstOrDefaultAsync(x => x.Id == Guid.Parse(id));

      ApplicationUser user = await _userManager.GetUserAsync(User);

      if (dbConversation == null)
      {
        throw new BadHttpRequestException(_localizer["Selected conversation doesn't exist"]);
      }

      Message approvalMessage = new Message()
      {
        Id = new Guid(),
        UserId = user.Id,
        ConversationId = dbConversation.Id,
        Status = NoteStatus.Rejected,
      };

      dbConversation.Messages.Add(_mapper.Map<DbMessage>(approvalMessage));
      _dbContext.SaveChanges();

      return Ok(_localizer["Conversation rejected successfully"]);
    }


    /// <summary>
    /// Approves conversation.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Operation satuts.</returns>
    [HttpPut("Approve")]
    public async Task<ActionResult> Approve([FromBody] Message message)
    {
      DbConversation dbConversation = await _dbContext.Conversations
        .Include(x => x.Messages)
        .FirstOrDefaultAsync(x => x.Id == message.ConversationId);

      ApplicationUser user = await _userManager.GetUserAsync(User);

      if (dbConversation == null)
      {
        throw new BadHttpRequestException(_localizer["Selected conversation doesn't exist"]);
      }

      if (message.Id == null)
      {
        message.Id = new Guid();
      }

      message.ConversationId = dbConversation.Id;
      message.UserId = user.Id;
      message.Status = NoteStatus.Approved;


      dbConversation.Messages.Add(_mapper.Map<DbMessage>(message));
      _dbContext.SaveChanges();

      return Ok(_localizer["Conversation approved successfully"]);
    }

    /// <summary>
    /// Rejects conversation.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Operation satuts.</returns>
    [HttpPut("Reject")]
    [Authorize(Roles =
        UserRoles.SUPERVISOR + "," +
        UserRoles.COORDINATOR + "," +
        UserRoles.ADMIN)]
    public async Task<ActionResult> Reject([FromBody] Message message)
    {
      DbConversation dbConversation = await _dbContext.Conversations
        .Include(x => x.Messages)
        .FirstOrDefaultAsync(x => x.Id == message.ConversationId);

      ApplicationUser user = await _userManager.GetUserAsync(User);

      if (dbConversation == null)
      {
        throw new BadHttpRequestException(_localizer["Selected conversation doesn't exist"]);
      }

      if (message.Id == null)
      {
        message.Id = new Guid();
      }

      message.ConversationId = dbConversation.Id;
      message.UserId = user.Id;
      message.Status = NoteStatus.Approved;


      dbConversation.Messages.Add(_mapper.Map<DbMessage>(message));
      _dbContext.SaveChanges();

      return Ok(_localizer["Conversation rejected successfully"]);
    }
  }
}