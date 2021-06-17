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
            using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (db.Stops.Count() > 0)
                        {
                            return;
                        }

                        long editorRoleId = 1;
                        long supervisorRoleId = 2;
                        long coordinatorRoleId = 3;
                        long uploaderRoleId = 4;
                        long adminRoleId = 5;

                        AddRole(db.Roles, editorRoleId, OsmIntegrator.Roles.UserRoles.EDITOR);
                        AddRole(db.Roles, supervisorRoleId, OsmIntegrator.Roles.UserRoles.SUPERVISOR);
                        AddRole(db.Roles, coordinatorRoleId, OsmIntegrator.Roles.UserRoles.COORDINATOR);
                        AddRole(db.Roles, uploaderRoleId, OsmIntegrator.Roles.UserRoles.UPLOADER);
                        AddRole(db.Roles, adminRoleId, OsmIntegrator.Roles.UserRoles.ADMIN);

                        AddUser(db.Users, db.UserRoles, 1, "user1@abcd.pl");
                        AddUser(db.Users, db.UserRoles, 2, "user2@abcd.pl");
                        AddUser(db.Users, db.UserRoles, 3, "user3@abcd.pl");
                        AddUser(db.Users, db.UserRoles, 4, "user4@abcd.pl");
                        AddUser(db.Users, db.UserRoles, 5, "editor1@abcd.pl", new List<long>() { editorRoleId });
                        AddUser(db.Users, db.UserRoles, 6, "editor2@abcd.pl", new List<long>() { editorRoleId });
                        AddUser(db.Users, db.UserRoles, 7, "supervisor1@abcd.pl", new List<long>() { supervisorRoleId });
                        AddUser(db.Users, db.UserRoles, 8, "supervisor2@abcd.pl", new List<long>() { supervisorRoleId, editorRoleId });
                        AddUser(db.Users, db.UserRoles, 9, "supervisor3@abcd.pl", new List<long>() { supervisorRoleId });
                        AddUser(db.Users, db.UserRoles, 10, "coordinator1@abcd.pl", new List<long>() { coordinatorRoleId });
                        AddUser(db.Users, db.UserRoles, 11, "coordinator2@abcd.pl", new List<long>() { editorRoleId, coordinatorRoleId, supervisorRoleId, uploaderRoleId });
                        AddUser(db.Users, db.UserRoles, 12, "uploader1@abcd.pl", new List<long>() { uploaderRoleId });
                        AddUser(db.Users, db.UserRoles, 13, "uploader2@abcd.pl", new List<long>() { supervisorRoleId, coordinatorRoleId });
                        AddUser(db.Users, db.UserRoles, 14, "admin@abcd.pl", new List<long>() { adminRoleId });

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
                                    connections.Add(new DbConnection() {
                                        GtfsStop = gtfsStop,
                                        OsmStop = osmStop,
                                        Imported = true
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

        public void AddRole(DbSet<ApplicationRole> roles, long id, string name)
        {
            roles.Add(new ApplicationRole() {
                Id = id,
                Name = name,
                NormalizedName = name.ToUpper()
            });
        }

        public void AddUser(
            DbSet<ApplicationUser> users,
            DbSet<IdentityUserRole<long>> userRoles,
            long id,
            string email,
            List<long> roleIds = null)
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

            foreach (long roleId in roleIds)
            {
                userRoles.Add(new IdentityUserRole<long>
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
            List<DbTag> tags = new List<DbTag>();

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
                    //node.Tag.ForEach(x => tags.Add(new Models.DbTag()
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
