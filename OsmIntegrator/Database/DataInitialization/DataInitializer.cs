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
using OsmIntegrator.Roles;
using Microsoft.EntityFrameworkCore;

namespace OsmIntegrator.Database.DataInitialization
{
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
            List<DbStop> gtfsStops = GetGtfsStopsList();
            List<DbStop> osmStops = GetOsmStopsList();

            Initialize(db, gtfsStops, osmStops);
        }

        public void Initialize(ApplicationDbContext db, List<DbStop> gtfsStops, List<DbStop> osmStops)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    if (db.Stops.Count() > 0)
                    {
                        return;
                    }

                    Guid editorRoleId = Guid.Parse("ff6cce18-706d-448c-89de-56212f1722ef");
                    Guid supervisorRoleId = Guid.Parse("3e4d73d6-6566-4518-a5b0-dce5a7016e24");
                    Guid coordinatorRoleId = Guid.Parse("17b91040-16cb-4fc6-9954-421cc8adc154");
                    Guid uploaderRoleId = Guid.Parse("f44243a2-7ea3-4327-9dbd-4ff97c164b33");
                    Guid adminRoleId = Guid.Parse("62f8bfff-d16a-4748-919c-56131148262e");

                    AddRole(db.Roles, editorRoleId, OsmIntegrator.Roles.UserRoles.EDITOR);
                    AddRole(db.Roles, supervisorRoleId, OsmIntegrator.Roles.UserRoles.SUPERVISOR);
                    AddRole(db.Roles, coordinatorRoleId, OsmIntegrator.Roles.UserRoles.COORDINATOR);
                    AddRole(db.Roles, uploaderRoleId, OsmIntegrator.Roles.UserRoles.UPLOADER);
                    AddRole(db.Roles, adminRoleId, OsmIntegrator.Roles.UserRoles.ADMIN);

                    AddUser(db.Users, db.UserRoles, Guid.Parse("c514ae13-d80a-40b1-90d0-df88ccca73ec"), "user1@abcd.pl", new List<Guid>() { editorRoleId });
                    AddUser(db.Users, db.UserRoles, Guid.Parse("9815ba57-e990-4f91-a8e9-17abe977d681"), "editor1@abcd.pl", new List<Guid>() { editorRoleId });
                    AddUser(db.Users, db.UserRoles, Guid.Parse("2afa8c7d-3b29-4d33-b69c-afc71626b109"), "supervisor1@abcd.pl", new List<Guid>() { supervisorRoleId });
                    AddUser(db.Users, db.UserRoles, Guid.Parse("58fb0db2-0d67-4aa4-af63-9e7111d0b346"), "supervisor2@abcd.pl", new List<Guid>() { supervisorRoleId, editorRoleId });
                    AddUser(db.Users, db.UserRoles, Guid.Parse("cbfbbb17-eaed-46e8-b972-8c9fd0f8fa5b"), "coordinator1@abcd.pl", new List<Guid>() { coordinatorRoleId });
                    AddUser(db.Users, db.UserRoles, Guid.Parse("55529e52-ba92-4727-94bd-53bcc1be06c8"), "admin@abcd.pl", new List<Guid>() { adminRoleId });
                    AddUser(db.Users, db.UserRoles, Guid.Parse("55529e52-ba92-4727-94bd-53bcc1be06c7"), "test@rozwiazaniadlaniewidomych.org", new List<Guid>() { editorRoleId, adminRoleId, supervisorRoleId });
                    AddUser(db.Users, db.UserRoles, Guid.Parse("a658f1c1-e91f-4bde-afb2-58c50b0d170a"), "editor2@abcd.pl", new List<Guid>() { editorRoleId });
                    db.SaveChanges();

                    List<DbStop> allStops = new List<DbStop>();
                    allStops.AddRange(osmStops);
                    allStops.AddRange(gtfsStops);
                    List<DbTile> tiles = GetTiles(allStops);

                    DbTile tile_2263_1385 = tiles.FirstOrDefault(x => x.X == 2263 && x.Y == 1385);

                    List<ApplicationUser> users = db.Users.Where(x => x.UserName.Contains("editor")).ToList();
                    ApplicationUser editor1 = users[0];
                    ApplicationUser editor2 = users[1];

                    List<ApplicationUser> supervisors = db.Users.Where(x => x.UserName.Contains("supervisor")).ToList();
                    ApplicationUser supervisor1 = supervisors[0];

                    tiles[0].Users = tiles[1].Users = tile_2263_1385.Users = new List<ApplicationUser> { editor1 };
                    tiles[0].EditorApproved = tiles[1].EditorApproved = tile_2263_1385.EditorApproved = editor1;
                    tiles[0].EditorApprovalTime = tiles[1].EditorApprovalTime = tile_2263_1385.EditorApprovalTime = DateTime.Now;

                    db.Stops.AddRange(allStops);
                    db.Tiles.AddRange(tiles);
                    db.SaveChanges();

                    DbTile tile = db.Tiles.First(x => x.X == 2263 && x.Y == 1385);

                    DbNote note1 = new DbNote
                    {
                        Text = "Not approved",
                        Lat = tile.MinLat,
                        Lon = tile.MinLon,
                        UserId = editor1.Id,
                        Status = NoteStatus.Created,
                        TileId = tile.Id
                    };

                    DbNote note2 = new DbNote
                    {
                        Text = "Approved",
                        Lat = tile.MaxLat,
                        Lon = tile.MinLon,
                        UserId = editor1.Id,
                        Status = NoteStatus.Approved,
                        Approver = supervisor1,
                        TileId = tile.Id
                    };

                    DbNote note3 = new DbNote
                    {
                        Text = "Supervisor approved",
                        Lat = tile.MaxLat,
                        Lon = tile.MinLon,
                        UserId = supervisor1.Id,
                        Status = NoteStatus.Rejected,
                        Approver = supervisor1,
                        TileId = tile.Id
                    };

                    DbNote note4 = new DbNote
                    {
                        Text = "Supervisor not approved",
                        Lat = tile.MaxLat,
                        Lon = tile.MinLon,
                        UserId = supervisor1.Id,
                        Status = NoteStatus.Created,
                        TileId = tile.Id
                    };

                    DbNote note5 = new DbNote
                    {
                        Text = "Editor 2",
                        Lat = tile.MaxLat,
                        Lon = tile.MinLon,
                        UserId = editor2.Id,
                        Status = NoteStatus.Created,
                        TileId = tile.Id
                    };

                    db.Notes.Add(note1);
                    db.Notes.Add(note2);
                    db.Notes.Add(note3);
                    db.Notes.Add(note4);
                    db.Notes.Add(note5);
                    db.SaveChanges();

                    List<DbConnections> connections = new List<DbConnections>();

                    foreach (DbStop osmStop in osmStops)
                    {
                        DbStop gtfsStop = gtfsStops.FirstOrDefault(x => x.StopId == osmStop.Ref);
                        if (gtfsStop != null)
                        {
                            connections.Add(new DbConnections()
                            {
                                GtfsStop = gtfsStop,
                                OsmStop = osmStop,
                                Imported = true
                            });
                        }
                    }

                    db.Connections.AddRange(connections);
                    db.SaveChanges();

                    transaction.Commit();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public void AddRole(DbSet<ApplicationRole> roles, Guid id, string name)
        {
            roles.Add(new ApplicationRole()
            {
                Id = id,
                Name = name,
                NormalizedName = name.ToUpper()
            });
        }

        public void AddUser(
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

        public List<DbStop> GetGtfsStopsList()
        {
            List<string[]> csvStopList = CsvParser.Parse("Files/GtfsStops.txt");
            List<DbStop> ztmStopList = csvStopList.Select((x, index) => new DbStop()
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

        public List<DbStop> GetOsmStopsList()
        {
            List<DbStop> result = new List<DbStop>();

            XmlSerializer serializer = new XmlSerializer(typeof(Osm));

            using (Stream reader = new FileStream("Files/OsmStops.xml", FileMode.Open))
            {
                Osm osmRoot = (Osm)serializer.Deserialize(reader);

                foreach (Node node in osmRoot.Node)
                {
                    DbStop stop = new DbStop
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

                    List<Models.Tag> tempTags = new List<Models.Tag>();

                    node.Tag.ForEach(x => tempTags.Add(new Models.Tag()
                    {
                        Key = x.K,
                        Value = x.V
                    }));
                    stop.Tags = tempTags;

                    var nameTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "name");
                    stop.Name = nameTag?.Value;

                    var refTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "ref");
                    long refVal;
                    if (refTag != null && long.TryParse(refTag.Value, out refVal))
                    {
                        stop.Ref = refVal;
                    }

                    var localRefTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "local_ref");
                    if (localRefTag != null && !string.IsNullOrEmpty(localRefTag.Value))
                    {
                        stop.Number = localRefTag.Value;
                    }

                    result.Add(stop);
                }
            }

            return result;
        }

        public List<DbTile> GetTiles(List<DbStop> stops)
        {
            Dictionary<Point<long>, DbTile> result = new Dictionary<Point<long>, DbTile>();

            foreach (DbStop stop in stops)
            {
                Point<long> tileXY = TilesHelper.WorldToTilePos(stop.Lon, stop.Lat, _zoomLevel);

                if (result.ContainsKey(tileXY))
                {
                    DbTile existingTile = result[tileXY];
                    stop.TileId = existingTile.Id;

                    if (stop.StopType == StopType.Gtfs)
                    {
                        existingTile.GtfsStopsCount++;
                    }
                    else
                    {
                        existingTile.OsmStopsCount++;
                    }

                    continue;
                }

                Point<double> leftUpperCorner = TilesHelper.TileToWorldPos(
                    tileXY.X, tileXY.Y, _zoomLevel
                );

                Point<double> rightBottomCorner = TilesHelper.TileToWorldPos(
                    tileXY.X + 1, tileXY.Y + 1, _zoomLevel
                );

                DbTile newTile = new DbTile(tileXY.X, tileXY.Y,
                    leftUpperCorner.X, rightBottomCorner.X,
                    rightBottomCorner.Y, leftUpperCorner.Y,
                    _overlapFactor);

                if (stop.StopType == StopType.Gtfs)
                {
                    newTile.GtfsStopsCount++;
                }
                else
                {
                    newTile.OsmStopsCount++;
                }

                stop.TileId = newTile.Id;
                result.Add(tileXY, newTile);
            }

            return result.Values.ToList();
        }

        public void ClearDatabase(ApplicationDbContext db)
        {
            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    db.Notes.RemoveRange(db.Notes);
                    db.Connections.RemoveRange(db.Connections);
                    db.Stops.RemoveRange(db.Stops);
                    db.Tiles.RemoveRange(db.Tiles);

                    db.Roles.RemoveRange(db.Roles);
                    db.Users.RemoveRange(db.Users);
                    db.UserRoles.RemoveRange(db.UserRoles);

                    db.SaveChanges();

                    transaction.Commit();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }
}
