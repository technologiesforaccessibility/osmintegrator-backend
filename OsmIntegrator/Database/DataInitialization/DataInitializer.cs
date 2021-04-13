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
            _overlapFactor = double.Parse(configuration["OverlapFactor"]);
        }

        public void Initialize(ApplicationDbContext db)
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

                        AddUser(db.Users, db.UserRoles, Guid.Parse("c514ae13-d80a-40b1-90d0-df88ccca73ec"), "user1@abcd.pl");
                        AddUser(db.Users, db.UserRoles, Guid.Parse("56164fad-3ac3-444f-a31a-b78104db193a"), "user2@abcd.pl");
                        AddUser(db.Users, db.UserRoles, Guid.Parse("54712763-695c-4efe-bbac-6f41403aab5a"), "user3@abcd.pl");
                        AddUser(db.Users, db.UserRoles, Guid.Parse("0c06b082-d711-4535-afab-3c94ff30be93"), "user4@abcd.pl");
                        AddUser(db.Users, db.UserRoles, Guid.Parse("9815ba57-e990-4f91-a8e9-17abe977d681"), "editor1@abcd.pl", new List<Guid>() { editorRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("a658f1c1-e91f-4bde-afb2-58c50b0d170a"), "editor2@abcd.pl", new List<Guid>() { editorRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("2afa8c7d-3b29-4d33-b69c-afc71626b109"), "supervisor1@abcd.pl", new List<Guid>() { supervisorRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("58fb0db2-0d67-4aa4-af63-9e7111d0b346"), "supervisor2@abcd.pl", new List<Guid>() { supervisorRoleId, editorRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("e06b45c4-2090-4bba-83f0-ae4cfeaf6669"), "supervisor3@abcd.pl", new List<Guid>() { supervisorRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("cbfbbb17-eaed-46e8-b972-8c9fd0f8fa5b"), "coordinator1@abcd.pl", new List<Guid>() { coordinatorRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("df03df13-6dd9-444f-81f9-2c7ac3229c26"), "coordinator2@abcd.pl", new List<Guid>() { editorRoleId, coordinatorRoleId, supervisorRoleId, uploaderRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("d117c862-a563-4baf-bb9f-fd1024ac71b0"), "uploader1@abcd.pl", new List<Guid>() { uploaderRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("ed694889-5518-47d2-86e5-a71052361673"), "uploader2@abcd.pl", new List<Guid>() { supervisorRoleId, coordinatorRoleId });
                        AddUser(db.Users, db.UserRoles, Guid.Parse("55529e52-ba92-4727-94bd-53bcc1be06c8"), "admin@abcd.pl", new List<Guid>() { adminRoleId });

                        db.SaveChanges();

                        List<DbStop> gtfsStops = GetGtfsStopsList();
                        (List<DbStop> osmStops, List<DbTag> osmTags) = GetOsmStopsList();
                        db.Tags.AddRange(osmTags);

                        List<DbStop> allStops = new List<DbStop>();
                        allStops.AddRange(osmStops);
                        allStops.AddRange(gtfsStops);
                        List<DbTile> tiles = GetTiles(allStops);

                        List<ApplicationUser> users = db.Users.Where(x => x.UserName.Contains("editor")).ToList();
                        ApplicationUser editor1 = users[0];
                        ApplicationUser editor2 = users[1];

                        tiles[0].Users = new List<ApplicationUser> { editor1 };
                        tiles[1].Users = new List<ApplicationUser> { editor1 };
                        tiles[2].Users = new List<ApplicationUser> { editor1 };

                        tiles[3].Users = new List<ApplicationUser> { editor2 };
                        tiles[4].Users = new List<ApplicationUser> { editor2 };
                        tiles[5].Users = new List<ApplicationUser> { editor2 };

                        db.Stops.AddRange(allStops);
                        db.Tiles.AddRange(tiles);
                        db.SaveChanges();

                        List<DbTag> tags = db.Tags.Where(x => x.Key == "ref").ToList();
                        List<DbStop> stops = db.Stops.Where(x => x.StopType == StopType.Gtfs).ToList();

                        List<DbConnection> connections = new List<DbConnection>();

                        foreach (DbTag tag in tags)
                        {
                            long stopId;
                            if (long.TryParse(tag.Value, out stopId))
                            {
                                DbStop gtfsStop = stops.FirstOrDefault(x => x.StopId == stopId);
                                if(gtfsStop != null)
                                {
                                    DbStop osmStop = tag.Stop;
                                    DbTile tile = osmStop.Tile;
                                    connections.Add(new DbConnection() {
                                        GtfsStop = gtfsStop,
                                        OsmStop = osmStop,
                                        Existing = true,
                                        Tile = tile
                                    });
                                }
                            }
                        }

                        db.Connections.AddRange(connections);
                        db.SaveChanges();

                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }
        }

        public void AddRole(DbSet<ApplicationRole> roles, Guid id, string name)
        {
            roles.Add(new ApplicationRole() {
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
                PasswordHash = hasher.HashPassword(null, $"12345678"),
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

        public (List<DbStop> Stops, List<Models.DbTag> Tags) GetOsmStopsList()
        {
            List<DbStop> result = new List<DbStop>();
            List<Models.DbTag> tags = new List<Models.DbTag>();

            XmlSerializer serializer =
                new XmlSerializer(typeof(Osm));

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
                        ProviderType = ProviderType.Ztm
                    };

                    List<Models.DbTag> tempTags = new List<Models.DbTag>();

                    node.Tag.ForEach(x => tempTags.Add(new Models.DbTag()
                    {
                        Id = Guid.NewGuid(),
                        StopId = stop.Id,
                        Key = x.K,
                        Value = x.V
                    }));

                    tags.AddRange(tempTags);

                    var nameTag = tempTags.FirstOrDefault(x => x.Key.ToLower() == "name");
                    stop.Name = nameTag?.Value;
                    result.Add(stop);
                }

            }

            return (result, tags);
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
    }
}
