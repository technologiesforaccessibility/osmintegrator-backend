using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using OsmIntegrator.Database;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using System.Linq;

namespace osmintegrator
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                logger.Debug("init main");
                IHost host = CreateHostBuilder(args).Build();

                InitializeData(host);

                host.Run();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Stopped program because of exception");
                throw;
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static void InitializeData(IHost host)
        {
            using (var scope = host.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.Migrate();

                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        if (db.Stops.Count() > 0)
                        {
                            return;
                        }

                        DataInitializer _dataInitializer = host.Services.GetService<DataInitializer>();

                        Guid editorRoleId = Guid.Parse("ff6cce18-706d-448c-89de-56212f1722ef");
                        Guid supervisorRoleId = Guid.Parse("3e4d73d6-6566-4518-a5b0-dce5a7016e24");
                        Guid coordinatorRoleId = Guid.Parse("17b91040-16cb-4fc6-9954-421cc8adc154");
                        Guid uploaderRoleId = Guid.Parse("f44243a2-7ea3-4327-9dbd-4ff97c164b33");
                        Guid adminRoleId = Guid.Parse("62f8bfff-d16a-4748-919c-56131148262e");

                        _dataInitializer.AddRole(db.Roles, editorRoleId, OsmIntegrator.Roles.UserRoles.EDITOR);
                        _dataInitializer.AddRole(db.Roles, supervisorRoleId, OsmIntegrator.Roles.UserRoles.SUPERVISOR);
                        _dataInitializer.AddRole(db.Roles, coordinatorRoleId, OsmIntegrator.Roles.UserRoles.COORDINATOR);
                        _dataInitializer.AddRole(db.Roles, uploaderRoleId, OsmIntegrator.Roles.UserRoles.UPLOADER);
                        _dataInitializer.AddRole(db.Roles, adminRoleId, OsmIntegrator.Roles.UserRoles.ADMIN);

                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("c514ae13-d80a-40b1-90d0-df88ccca73ec"), "user1@abcd.pl");
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("56164fad-3ac3-444f-a31a-b78104db193a"), "user2@abcd.pl");
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("54712763-695c-4efe-bbac-6f41403aab5a"), "user3@abcd.pl");
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("0c06b082-d711-4535-afab-3c94ff30be93"), "user4@abcd.pl");
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("9815ba57-e990-4f91-a8e9-17abe977d681"), "editor1@abcd.pl", new List<Guid>() { editorRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("a658f1c1-e91f-4bde-afb2-58c50b0d170a"), "editor2@abcd.pl", new List<Guid>() { editorRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("2afa8c7d-3b29-4d33-b69c-afc71626b109"), "supervisor1@abcd.pl", new List<Guid>() { supervisorRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("58fb0db2-0d67-4aa4-af63-9e7111d0b346"), "supervisor2@abcd.pl", new List<Guid>() { supervisorRoleId, editorRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("e06b45c4-2090-4bba-83f0-ae4cfeaf6669"), "supervisor3@abcd.pl", new List<Guid>() { supervisorRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("cbfbbb17-eaed-46e8-b972-8c9fd0f8fa5b"), "coordinator1@abcd.pl", new List<Guid>() { coordinatorRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("df03df13-6dd9-444f-81f9-2c7ac3229c26"), "coordinator2@abcd.pl", new List<Guid>() { editorRoleId, coordinatorRoleId, supervisorRoleId, uploaderRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("d117c862-a563-4baf-bb9f-fd1024ac71b0"), "uploader1@abcd.pl", new List<Guid>() { uploaderRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("ed694889-5518-47d2-86e5-a71052361673"), "uploader2@abcd.pl", new List<Guid>() { supervisorRoleId, coordinatorRoleId });
                        _dataInitializer.AddUser(db.Users, db.UserRoles, Guid.Parse("55529e52-ba92-4727-94bd-53bcc1be06c8"), "admin@abcd.pl", new List<Guid>() { adminRoleId });

                        db.SaveChanges();

                        List<DbStop> allStops = _dataInitializer.GetGtfsStopsList();
                        (List<DbStop> Stops, List<DbTag> Tags) = _dataInitializer.GetOsmStopsList();
                        allStops.AddRange(Stops);
                        List<DbTile> tiles = _dataInitializer.GetTiles(allStops);

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
                        transaction.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction.Rollback();
                    }
                }
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                }).UseNLog();
    }
}
