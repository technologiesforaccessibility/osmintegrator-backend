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
using OsmIntegrator.Database.Models.Enums;
using System.Text;

namespace OsmIntegrator.Database.DataInitialization
{
    public class DataInitializer
    {
        private readonly double _overlapFactor;
        private readonly int _zoomLevel;

        private DbCategory _category;
        private DbField _idField;
        private DbField _numberField;
        private DbField _latitudeField;
        private DbField _longitudeField;
        private DbField _nameField;
        private DbField _tagsField;
        private DbField _connectionField;

        private DbVersion _osmInitialVersion;
        private DbVersion _gtfsInitialVersion;

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
                    AddUser(db.Users, db.UserRoles, 15, "gtfs@rozwiazaniadlaniewidomych.org", new List<long>() { });
                    AddUser(db.Users, db.UserRoles, 16, "osm@rozwiazaniadlaniewidomych.org", new List<long>() { });

                    db.SaveChanges();

                    ApplicationUser gtfsUser = db.Users.First(x => x.Id == 15);
                    ApplicationUser osmUser = db.Users.First(x => x.Id == 16);

                    InitializeVersions(db, gtfsUser, osmUser);
                    InitializeCategories(db);
                    InitializeFields(db);

                    GetGtfsStopsList(db);
                    GetOsmStopsList(db);

                    

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
                            if (gtfsStop != null)
                            {
                                DbStop osmStop = tag.Stop;
                                connections.Add(new DbConnection()
                                {
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

        private void InitializeVersions(
            ApplicationDbContext db,
            ApplicationUser gtfsUser,
            ApplicationUser osmUser)
        {
            DbBranch branch = new DbBranch
            {
                Name = "develop"
            };
            db.SaveChanges();

            _gtfsInitialVersion = new DbVersion
            {
                User = gtfsUser,
                Description = "GtfsInitial",
                CreatedAt = DateTime.Now,
                Branch = branch
            };
            db.Versions.Add(_gtfsInitialVersion);

            _osmInitialVersion = new DbVersion
            {
                User = osmUser,
                Description = "OsmInitial",
                CreatedAt = DateTime.Now,
                Branch = branch
            };
            db.Versions.Add(_osmInitialVersion);

            db.SaveChanges();

            DbVersionConnector versionConnector = new DbVersionConnector
            {
                Parent = _gtfsInitialVersion,
                Child = _osmInitialVersion
            };
        }

        private void InitializeCategories(ApplicationDbContext db)
        {
            _category = new DbCategory
            {
                Name = "Basic"
            };

            db.Categories.Add(_category);
            db.SaveChanges();
        }

        private void InitializeFields(ApplicationDbContext db)
        {
            _idField = new DbField
            {
                Category = _category,
                FieldType = FieldType.Long,
                Name = "Id",
            };

            _numberField = new DbField
            {
                Category = _category,
                FieldType = FieldType.String,
                Name = "Number",
            };

            _latitudeField = new DbField
            {
                Category = _category,
                FieldType = FieldType.Double,
                Name = "Latitude",
            };

            _longitudeField = new DbField
            {
                Category = _category,
                FieldType = FieldType.Double,
                Name = "Longitude",
            };

            _nameField = new DbField
            {
                Category = _category,
                FieldType = FieldType.String,
                Name = "Name",
            };

            _tagsField = new DbField
            {
                Category = _category,
                FieldType = FieldType.Tags,
                Name = "Tags",
            };

            _connectionField = new DbField
            {
                Category = _category,
                FieldType = FieldType.Long,
                Name = "Connection",
            };

            db.Fields.Add(_idField);
            db.Fields.Add(_numberField);
            db.Fields.Add(_latitudeField);
            db.Fields.Add(_longitudeField);
            db.Fields.Add(_nameField);
            db.Fields.Add(_tagsField);
            db.Fields.Add(_connectionField);

            db.SaveChanges();
        }

        public void AddRole(DbSet<ApplicationRole> roles, long id, string name)
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

        public void GetGtfsStopsList(ApplicationDbContext db)
        {
            DbObjectType gtfsObjectType = new DbObjectType
            {
                Name = "GTFS",
            };
            db.ObjectTypes.Add(gtfsObjectType);
            db.SaveChanges();

            List<DbObject> objects = new List<DbObject>();

            List<DbValue> values = new List<DbValue>();

            List<string[]> csvStopList = CsvParser.Parse("Files/GtfsStops.txt");
            foreach (string[] x in csvStopList)
            {
                DbObject currentObject = new DbObject
                {
                    ObjectType = gtfsObjectType
                };

                objects.Add(currentObject);

                values.Add(new DbValue
                {
                    Field = _idField,
                    LongValue = long.Parse(x[0]),
                    Version = _gtfsInitialVersion,
                    Object = currentObject
                });

                values.Add(new DbValue
                {
                    Field = _numberField,
                    StringValue = x[1],
                    Version = _gtfsInitialVersion,
                    Object = currentObject
                });

                values.Add(new DbValue
                {
                    Field = _nameField,
                    StringValue = x[2],
                    Version = _gtfsInitialVersion,
                    Object = currentObject
                });

                values.Add(new DbValue
                {
                    Field = _latitudeField,
                    DoubleValue = double.Parse(x[4], CultureInfo.InvariantCulture.NumberFormat),
                    Version = _gtfsInitialVersion,
                    Object = currentObject
                });

                values.Add(new DbValue
                {
                    Field = _longitudeField,
                    DoubleValue = double.Parse(x[5], CultureInfo.InvariantCulture.NumberFormat),
                    Version = _gtfsInitialVersion,
                    Object = currentObject
                });
            }
        }

        public void GetOsmStopsList(ApplicationDbContext db)
        {
            DbObjectType osmObjectType = new DbObjectType
            {
                Name = "OSM",
            };
            db.ObjectTypes.Add(osmObjectType);
            db.SaveChanges();

            List<DbObject> objects = new List<DbObject>();
            List<DbValue> values = new List<DbValue>();
            List<DbValue> connections = new List<DbValue>();

            XmlSerializer serializer =
                new XmlSerializer(typeof(Osm));

            using (Stream reader = new FileStream("Files/OsmStops.xml", FileMode.Open))
            {
                Osm osmRoot = (Osm)serializer.Deserialize(reader);

                foreach (Node node in osmRoot.Node)
                {
                    DbObject currentObject = new DbObject
                    {
                        ObjectType = osmObjectType
                    };

                    objects.Add(currentObject);

                    values.Add(new DbValue
                    {
                        Field = _idField,
                        LongValue = long.Parse(node.Id),
                        Version = _osmInitialVersion,
                        Object = currentObject
                    });

                    values.Add(new DbValue
                    {
                        Field = _latitudeField,
                        DoubleValue = double.Parse(node.Lat, CultureInfo.InvariantCulture),
                        Version = _osmInitialVersion,
                        Object = currentObject
                    });

                    values.Add(new DbValue
                    {
                        Field = _longitudeField,
                        DoubleValue = double.Parse(node.Lon, CultureInfo.InvariantCulture),
                        Version = _osmInitialVersion,
                        Object = currentObject
                    });

                    StringComparer comparer = StringComparer.OrdinalIgnoreCase;
                    Dictionary<string, string> tags = new Dictionary<string, string>(comparer);
                    node.Tag.ForEach(x => tags.Add(x.K, x.V));

                    StringBuilder sb = new StringBuilder();
                    foreach (var tag in tags)
                    {
                        sb.AppendLine($"{tag.Key}={tag.Value}");
                    }
                    values.Add(new DbValue
                    {
                        Field = _tagsField,
                        StringValue = sb.ToString(),
                        Version = _osmInitialVersion,
                        Object = currentObject
                    });

                    string stopName;
                    tags.TryGetValue("name", out stopName);
                    if (stopName != null)
                    {
                        values.Add(new DbValue
                        {
                            Field = _nameField,
                            StringValue = stopName,
                            Version = _osmInitialVersion,
                            Object = currentObject
                        });
                    }

                    string stopNumber;
                    tags.TryGetValue("local_ref", out stopNumber);
                    if (stopNumber != null)
                    {
                        values.Add(new DbValue
                        {
                            Field = _nameField,
                            StringValue = stopName,
                            Version = _osmInitialVersion,
                            Object = currentObject
                        });
                    }

                    // Add connection
                    string gtfsStopId;
                    tags.TryGetValue("ref", out gtfsStopId);
                    if (gtfsStopId != null)
                    {
                        long gtfsId = long.Parse(gtfsStopId);

                        DbObjectType gtfsObjectType = db.ObjectTypes.First(x => x.Name == "GTFS");
                        DbValue value = db.Values.Include(x => x.Object)
                            .FirstOrDefault(x => x.Object.ObjectType.Id == gtfsObjectType.Id &&
                                x.Field.Id == _idField.Id &&
                                x.LongValue == gtfsId);

                        if (value != null)
                        {
                            connections.Add(new DbValue
                            {
                                Field = _connectionField,
                                RelatedObject = currentObject,
                                Version = _gtfsInitialVersion,
                                Object = value.Object
                            });
                        }
                    }
                }
            }

            db.Objects.AddRange(objects);

            db.SaveChanges();

            db.Values.AddRange(values);

            db.SaveChanges();

            db.Values.AddRange(connections);

            db.SaveChanges();
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
