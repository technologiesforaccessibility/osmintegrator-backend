using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using OsmIntegrator.Database.Models;
using System.Xml.Serialization;
using OsmIntegrator.Tools;
using System.IO;
using System;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Enums;

namespace OsmIntegrator.Database.DataInitialization;

public class DataInitializer
{
  private readonly double _overlapFactor;
  private readonly int _zoomLevel;

  public DataInitializer(IConfiguration configuration)
  {
    _zoomLevel = int.Parse(configuration["ZoomLevel"]);
    _overlapFactor = double.Parse(configuration["OverlapFactor"], NumberFormatInfo.InvariantInfo);
  }

  public void Initialize(ApplicationDbContext db)
  {
    List<DbStop> gtfsStops = GetGtfsStopsList("Files/GtfsStops.txt");
    List<DbStop> osmStops = GetOsmStopsList("Files/OsmStops.xml");

    Initialize(db, gtfsStops, osmStops);
  }

  public void InitializeUsers(ApplicationDbContext db)
  {
    Guid editorRoleId = Guid.Parse("ff6cce18-706d-448c-89de-56212f1722ef");
    Guid supervisorRoleId = Guid.Parse("3e4d73d6-6566-4518-a5b0-dce5a7016e24");
    Guid coordinatorRoleId = Guid.Parse("17b91040-16cb-4fc6-9954-421cc8adc154");
    Guid uploaderRoleId = Guid.Parse("f44243a2-7ea3-4327-9dbd-4ff97c164b33");
    Guid adminRoleId = Guid.Parse("62f8bfff-d16a-4748-919c-56131148262e");

    AddRole(db.Roles, editorRoleId, Roles.UserRoles.EDITOR);
    AddRole(db.Roles, supervisorRoleId, Roles.UserRoles.SUPERVISOR);
    AddRole(db.Roles, coordinatorRoleId, Roles.UserRoles.COORDINATOR);
    AddRole(db.Roles, uploaderRoleId, Roles.UserRoles.UPLOADER);
    AddRole(db.Roles, adminRoleId, Roles.UserRoles.ADMIN);

    AddUser(db.Users, db.UserRoles, Guid.Parse("c514ae13-d80a-40b1-90d0-df88ccca73ec"), 
      "user1@abcd.pl", new List<Guid>() { editorRoleId });
    AddUser(db.Users, db.UserRoles, Guid.Parse("9815ba57-e990-4f91-a8e9-17abe977d681"), 
      "editor1@abcd.pl", new List<Guid>() { editorRoleId });
    AddUser(db.Users, db.UserRoles, Guid.Parse("2afa8c7d-3b29-4d33-b69c-afc71626b109"), 
      "supervisor1@abcd.pl", new List<Guid>() { supervisorRoleId });
    AddUser(db.Users, db.UserRoles, Guid.Parse("58fb0db2-0d67-4aa4-af63-9e7111d0b346"), 
      "supervisor2@abcd.pl", new List<Guid>() { supervisorRoleId, editorRoleId });
    AddUser(db.Users, db.UserRoles, Guid.Parse("cbfbbb17-eaed-46e8-b972-8c9fd0f8fa5b"), 
      "coordinator1@abcd.pl", new List<Guid>() { coordinatorRoleId });
    AddUser(db.Users, db.UserRoles, Guid.Parse("55529e52-ba92-4727-94bd-53bcc1be06c8"), 
      "admin@abcd.pl", new List<Guid>() { adminRoleId });
    AddUser(db.Users, db.UserRoles, Guid.Parse("55529e52-ba92-4727-94bd-53bcc1be06c7"), 
      "test@rozwiazaniadlaniewidomych.org", new List<Guid>() { editorRoleId, adminRoleId, supervisorRoleId });
    AddUser(db.Users, db.UserRoles, Guid.Parse("a658f1c1-e91f-4bde-afb2-58c50b0d170a"), 
      "editor2@abcd.pl", new List<Guid>() { editorRoleId });
    
    db.SaveChanges();
  }

  public void InitializeStopsAndTiles(
    ApplicationDbContext db, List<DbStop> gtfsStops, List<DbStop> osmStops)
  {
    gtfsStops ??= new();
    osmStops ??= new();

    List<DbStop> allStops = new List<DbStop>();
    allStops.AddRange(osmStops);
    allStops.AddRange(gtfsStops);
    List<DbTile> tiles = GetTiles(allStops);

    db.Stops.AddRange(allStops);
    db.Tiles.AddRange(tiles);

    InitializeOsmConnections(db, gtfsStops, osmStops);

    db.SaveChanges();
  }

  private void InitializeOsmConnections(ApplicationDbContext db, List<DbStop> gtfsStops, List<DbStop> osmStops)
  {
    if(gtfsStops == null || osmStops == null) return;

    ApplicationUser supervisor = db.Users.First(x => x.UserName == "supervisor2");

    List<DbConnection> connections = new();

    foreach (DbStop osmStop in osmStops)
    {
      if (long.TryParse(osmStop.Ref, out long @ref))
      {
        DbStop gtfsStop = gtfsStops.FirstOrDefault(x => x.StopId == @ref);

        if (gtfsStop != null)
        {
          connections.Add(new DbConnection()
          {
            GtfsStop = gtfsStop,
            OsmStop = osmStop,
            UserId = supervisor.Id,
            OperationType = ConnectionOperationType.Added
          });
        }
      }
    }

    db.Connections.AddRange(connections);
    db.SaveChanges();
  }

  private void Initialize(ApplicationDbContext db, List<DbStop> gtfsStops, List<DbStop> osmStops)
  {
    using IDbContextTransaction transaction = db.Database.BeginTransaction();
    try
    {
      if (db.Stops.Any())
      {
        return;
      }

      InitializeUsers(db);
      InitializeStopsAndTiles(db, gtfsStops, osmStops);
      InitializeOsmConnections(db, gtfsStops, osmStops);

      transaction.Commit();
    }
    catch (Exception e)
    {
      Console.WriteLine(e);
      transaction.Rollback();
      throw;
    }
  }

  private void AddRole(DbSet<ApplicationRole> roles, Guid id, string name)
  {
    roles.Add(new ApplicationRole()
    {
      Id = id,
      Name = name,
      NormalizedName = name.ToUpper()
    });
  }

  private void AddUser(
    DbSet<ApplicationUser> users,
    DbSet<IdentityUserRole<Guid>> userRoles,
    Guid id,
    string email,
    List<Guid> roleIds = null)
  {
    var hasher = new PasswordHasher<ApplicationUser>();
    string name = email.Split("@")[0];
    users.Add(new ApplicationUser
    {
      Id = id,
      UserName = name,
      NormalizedUserName = name.ToUpper(),
      Email = email,
      NormalizedEmail = email.ToUpper(),
      EmailConfirmed = true,
      PasswordHash = hasher.HashPassword(null, $"{name}#12345678"),
      SecurityStamp = string.Empty
    });

    if (roleIds == null) return;

    foreach (Guid roleId in roleIds)
    {
      userRoles.Add(new IdentityUserRole<Guid>
      {
        RoleId = roleId,
        UserId = id
      });
    }
  }

  public static List<DbStop> GetGtfsStopsList(string fileName)
  {
    List<string[]> csvStopList = CsvParser.Parse(fileName);
    List<DbStop> ztmStopList = csvStopList.Select((x, _) => new DbStop()
    {
      Id = Guid.NewGuid(),
      StopId = long.Parse(x[0]),
      Number = x[1],
      Name = x[2],
      Lat = double.Parse(x[4], CultureInfo.InvariantCulture.NumberFormat),
      Lon = double.Parse(x[5], CultureInfo.InvariantCulture.NumberFormat),
      StopType = StopType.Gtfs,
      ProviderType = ProviderType.Ztm
    }).ToList();
    return ztmStopList;
  }

  public List<DbStop> GetOsmStopsList(string fileName)
  {
    List<DbStop> result = new();

    XmlSerializer serializer = new(typeof(Osm));

    using (Stream reader = new FileStream(fileName, FileMode.Open))
    {
      Osm osmRoot = (Osm)serializer.Deserialize(reader);

      foreach (Node node in osmRoot!.Node)
      {
        DbStop stop = new()
        {
          Id = Guid.NewGuid(),
          StopId = long.Parse(node.Id),
          Lat = double.Parse(node.Lat, CultureInfo.InvariantCulture),
          Lon = double.Parse(node.Lon, CultureInfo.InvariantCulture),
          StopType = StopType.Osm,
          ProviderType = ProviderType.Ztm,
          Version = node.Version,
          Changeset = node.Changeset
        };

        List<Models.JsonFields.Tag> tempTags = new();

        node.Tag.ForEach(x => tempTags.Add(new Models.JsonFields.Tag()
        {
          Key = x.K,
          Value = x.V
        }));
        stop.Tags = tempTags;

        var nameTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == Constants.NAME);
        stop.Name = nameTag?.Value;

        var refTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == Constants.REF);
        stop.Ref = refTag?.Value;

        var localRefTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == Constants.LOCAL_REF);
        stop.Number = localRefTag?.Value;

        result.Add(stop);
      }
    }

    return result;
  }

  private List<DbTile> GetTiles(List<DbStop> stops)
  {
    Dictionary<Point<long>, DbTile> result = new();

    foreach (DbStop stop in stops)
    {
      Point<long> tileCoordinates = TilesHelper.WorldToTilePos(stop.Lon, stop.Lat, _zoomLevel);

      if (result.ContainsKey(tileCoordinates))
      {
        DbTile existingTile = result[tileCoordinates];
        stop.TileId = existingTile.Id;

        continue;
      }

      Point<double> leftUpperCorner = TilesHelper.TileToWorldPos(
        tileCoordinates.X, tileCoordinates.Y, _zoomLevel
      );

      Point<double> rightBottomCorner = TilesHelper.TileToWorldPos(
        tileCoordinates.X + 1, tileCoordinates.Y + 1, _zoomLevel
      );

      DbTile newTile = new(tileCoordinates.X, tileCoordinates.Y,
        leftUpperCorner.X, rightBottomCorner.X,
        rightBottomCorner.Y, leftUpperCorner.Y,
        _overlapFactor);

      stop.TileId = newTile.Id;
      result.Add(tileCoordinates, newTile);
    }

    return result.Values.ToList();
  }

  public void ClearDatabase(ApplicationDbContext db)
  {
    db.Connections.RemoveRange(db.Connections);
    db.Stops.RemoveRange(db.Stops);
    db.Tiles.RemoveRange(db.Tiles);

    db.Conversations.RemoveRange(db.Conversations);
    db.Messages.RemoveRange(db.Messages);

    db.Roles.RemoveRange(db.Roles);
    db.Users.RemoveRange(db.Users);
    db.UserRoles.RemoveRange(db.UserRoles);

    db.ChangeReports.RemoveRange(db.ChangeReports);

    db.SaveChanges();
  }
}