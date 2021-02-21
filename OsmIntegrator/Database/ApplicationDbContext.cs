using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OsmIntegrator.Database.DataInitialization;
using OsmIntegrator.Database.Models;
using OsmIntegrator.ApiModels;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;
using System;

namespace OsmIntegrator.Database
{
    public class ApplicationDbContext :
        IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        private IConfiguration _configuration;

        public DbSet<DbStop> Stops { get; set; }

        public DbSet<DbTag> Tags { get; set; }

        public DbSet<DbTile> Tiles { get; set; }

        private DataInitializer _dataInitializer { get; set; }

        public ApplicationDbContext(IConfiguration configuration, DataInitializer dataInitializer)
        {
            _configuration = configuration;
            _dataInitializer = dataInitializer;
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            _ = optionsBuilder.UseNpgsql(GetConnectionString());
        }
        public string GetConnectionString()
        {
            return _configuration["DBConnectionString"].ToString();
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            Guid editorRoleId = Guid.Parse("ff6cce18-706d-448c-89de-56212f1722ef");
            Guid supervisorRoleId = Guid.Parse("3e4d73d6-6566-4518-a5b0-dce5a7016e24");
            Guid coordinatorRoleId = Guid.Parse("17b91040-16cb-4fc6-9954-421cc8adc154");
            Guid uploaderRoleId = Guid.Parse("f44243a2-7ea3-4327-9dbd-4ff97c164b33");
            Guid adminRoleId = Guid.Parse("62f8bfff-d16a-4748-919c-56131148262e");

            _dataInitializer.AddRole(modelBuilder, editorRoleId, OsmIntegrator.Roles.UserRoles.EDITOR);
            _dataInitializer.AddRole(modelBuilder, supervisorRoleId, OsmIntegrator.Roles.UserRoles.SUPERVISOR);
            _dataInitializer.AddRole(modelBuilder, coordinatorRoleId, OsmIntegrator.Roles.UserRoles.COORDINATOR);
            _dataInitializer.AddRole(modelBuilder, uploaderRoleId, OsmIntegrator.Roles.UserRoles.UPLOADER);
            _dataInitializer.AddRole(modelBuilder, adminRoleId, OsmIntegrator.Roles.UserRoles.ADMIN);

            _dataInitializer.AddUser(modelBuilder, Guid.Parse("c514ae13-d80a-40b1-90d0-df88ccca73ec"), "user1@abcd.pl");
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("56164fad-3ac3-444f-a31a-b78104db193a"), "user2@abcd.pl");
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("54712763-695c-4efe-bbac-6f41403aab5a"), "user3@abcd.pl");
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("0c06b082-d711-4535-afab-3c94ff30be93"), "user4@abcd.pl");
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("9815ba57-e990-4f91-a8e9-17abe977d681"), "editor1@abcd.pl", new List<Guid>() { editorRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("a658f1c1-e91f-4bde-afb2-58c50b0d170a"), "editor2@abcd.pl", new List<Guid>() { editorRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("2afa8c7d-3b29-4d33-b69c-afc71626b109"), "supervisor1@abcd.pl", new List<Guid>() { supervisorRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("58fb0db2-0d67-4aa4-af63-9e7111d0b346"), "supervisor2@abcd.pl", new List<Guid>() { supervisorRoleId, editorRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("e06b45c4-2090-4bba-83f0-ae4cfeaf6669"), "supervisor3@abcd.pl", new List<Guid>() { supervisorRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("cbfbbb17-eaed-46e8-b972-8c9fd0f8fa5b"), "coordinator1@abcd.pl", new List<Guid>() { coordinatorRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("df03df13-6dd9-444f-81f9-2c7ac3229c26"), "coordinator2@abcd.pl", new List<Guid>() { editorRoleId, coordinatorRoleId, supervisorRoleId, uploaderRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("d117c862-a563-4baf-bb9f-fd1024ac71b0"), "uploader1@abcd.pl", new List<Guid>() { uploaderRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("ed694889-5518-47d2-86e5-a71052361673"), "uploader2@abcd.pl", new List<Guid>() { supervisorRoleId, coordinatorRoleId });
            _dataInitializer.AddUser(modelBuilder, Guid.Parse("55529e52-ba92-4727-94bd-53bcc1be06c8"), "admin@abcd.pl", new List<Guid>() { adminRoleId });

            List<DbStop> allStops = _dataInitializer.GetGtfsStopsList();

            (List<DbStop> Stops, List<DbTag> Tags) = _dataInitializer.GetOsmStopsList();

            allStops.AddRange(Stops);

            List<DbTile> tiles = _dataInitializer.GetTiles(allStops);

            modelBuilder.Entity<DbStop>().HasData(allStops);
            modelBuilder.Entity<DbTag>().HasData(Tags);
            modelBuilder.Entity<DbTile>().HasData(tiles);

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ApplicationUserRole>(entity =>
            {
                entity
                .HasOne(x => x.Role)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.RoleId);

                entity
                .HasOne(x => x.User)
                .WithMany(x => x.UserRoles)
                .HasForeignKey(x => x.UserId);
            });
        }
    }
}
