using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System.Net.Mime;
using Microsoft.AspNetCore.Cors;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database;
using OsmIntegrator.ApiModels;
using OsmIntegrator.Interfaces;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.ApiModels.Conversation;

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
  public class ConversationController : Controller
  {
    private readonly ApplicationDbContext _dbContext;

    private readonly ILogger<ConversationController> _logger;

    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<ApplicationRole> _roleManager;

    private readonly IMapper _mapper;
    private readonly IStringLocalizer<ConversationController> _localizer;

    private readonly IConfiguration _configuration;

    public ConversationController(
        ILogger<ConversationController> logger,
        IMapper mapper,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext dbContext,
        IStringLocalizer<ConversationController> localizer,
        IConfiguration configuration
    )
    {
      _logger = logger;
      _userManager = userManager;
      _mapper = mapper;
      _roleManager = roleManager;
      _dbContext = dbContext;
      _localizer = localizer;
      _configuration = configuration;
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
    public async Task<ActionResult> AddMessage([FromBody] MessageInput messageInput)
    {
      ApplicationUser user = await _userManager.GetUserAsync(User);
      Message message = new Message()
      {
        UserId = user.Id,
        Status = NoteStatus.Created,
        Text = messageInput.Text,
        CreatedAt = DateTime.Now
      };

      DbMessage dbMessage = _mapper.Map<DbMessage>(message);
      dbMessage.User = user;

      if (messageInput.ConversationId == null && messageInput.StopId == null && (messageInput.Lat == null || messageInput.Lon == null))
      {
        throw new BadHttpRequestException(_localizer["Incorrect conversation data"]);
      }

      if (messageInput.ConversationId == null)
      {
        DbConversation dbConversation = _mapper.Map<DbConversation>(new Conversation()
        {
          StopId = messageInput.StopId,
          Lat = messageInput.Lat,
          Lon = messageInput.Lon,
          TileId = (Guid)messageInput.TileId,
          Id = new Guid(),
        });
        await _dbContext.AddAsync(dbConversation);

        dbMessage.Conversation = dbConversation;
      }
      else
      {
        dbMessage.ConversationId = (Guid)messageInput.ConversationId;
      }
      await _dbContext.AddAsync(dbMessage);

      _dbContext.SaveChanges();

      return Ok(_localizer["Message successfully added"]);
    }

    /// <summary>
    /// It returns all conversations situated at the specific tile including conversations on the overlapped positions.
    /// </summary>
    /// <param name="tileId">Tile id</param>
    /// <returns>List with conversations.</returns>
    [HttpGet("{tileId}")]
    [Authorize(Roles =
        UserRoles.EDITOR + "," +
        UserRoles.SUPERVISOR + "," +
        UserRoles.COORDINATOR + "," +
        UserRoles.ADMIN)]
    public async Task<ActionResult<ConversationResponse>> Get(string tileId)
    {
      DbTile tile = await _dbContext.Tiles.
          FirstOrDefaultAsync(x => x.Id == Guid.Parse(tileId));

      if (tile == null)
      {
        throw new BadHttpRequestException(_localizer["Tile doesn't exist"]);
      }
      ApplicationUser user = await _userManager.GetUserAsync(User);

      List<DbConversation> dbStopConversations = await _dbContext.Conversations
        .Include(x => x.Messages)
          .ThenInclude(y => y.User)
        .Where(x => x.TileId == tile.Id && x.StopId != null && x.Messages.Count() != 0)
        .ToListAsync();

      List<DbConversation> dbGeoConversations = await _dbContext.Conversations
      .Include(x => x.Messages)
        .ThenInclude(y => y.User)
      .Where(x => x.TileId == tile.Id && x.Lat != null && x.Lon != null && x.Messages.Count() != 0)
      .ToListAsync();

      ConversationResponse response = new ConversationResponse()
      {
        GeoConversations = _mapper.Map<List<Conversation>>(dbGeoConversations),
        StopConversations = _mapper.Map<List<Conversation>>(dbStopConversations),
      };


      return Ok(response);
    }

    /// <summary>
    /// Approves conversation.
    /// </summary>
    /// <param name="message">Message object.</param>
    /// <returns>Operation satuts.</returns>
    [HttpPut("Approve")]
    [Authorize(Roles =
        UserRoles.EDITOR + "," +
        UserRoles.SUPERVISOR + "," +
        UserRoles.COORDINATOR + "," +
        UserRoles.ADMIN)]
    public async Task<ActionResult> Approve([FromBody] MessageInput messageInput)
    {
      DbConversation dbConversation = await _dbContext.Conversations
        .Include(x => x.Messages)
          .ThenInclude(y => y.User)
        .FirstOrDefaultAsync(x => x.Id == messageInput.ConversationId);

      ApplicationUser user = await _userManager.GetUserAsync(User);

      if (dbConversation == null)
      {
        throw new BadHttpRequestException(_localizer["Selected conversation doesn't exist"]);
      }
      Message message = new Message()
      {
        Id = new Guid(),
        UserId = user.Id,
        ConversationId = dbConversation.Id,
        Status = NoteStatus.Approved,
        CreatedAt = DateTime.Now,
        Text = messageInput.Text,
      };
      DbMessage dbMessage = _mapper.Map<DbMessage>(message);
      dbMessage.User = user;

      dbConversation.Messages.Add(dbMessage);

      _dbContext.SaveChanges();

      return Ok(_localizer["Conversation approved successfully"]);
    }
  }
}