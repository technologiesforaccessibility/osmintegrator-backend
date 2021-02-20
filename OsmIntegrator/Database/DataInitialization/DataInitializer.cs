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

        public void AddRole(ModelBuilder modelBuilder, string id, string name)
        {
            modelBuilder.Entity<IdentityRole>().HasData(new IdentityRole
            {
                Id = id,
                Name = name,
                NormalizedName = name.ToUpper()
            });
        }

        public void AddUser(
            ModelBuilder modelBuilder,
            string id,
            string email,
            List<string> roleIds = null)
        {
            var hasher = new PasswordHasher<IdentityUser>();
            string name = email.Split("@")[0];
            modelBuilder.Entity<IdentityUser>().HasData(new IdentityUser
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

            foreach (string roleId in roleIds)
            {
                modelBuilder.Entity<IdentityUserRole<string>>().HasData(new IdentityUserRole<string>
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
                        OsmStopId = stop.Id,
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
