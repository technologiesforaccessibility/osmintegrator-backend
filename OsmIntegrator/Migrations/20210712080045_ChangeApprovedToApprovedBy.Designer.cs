﻿// <auto-generated />
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using OsmIntegrator.Database;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;

namespace osmintegrator.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20210712080045_ChangeApprovedToApprovedBy")]
    partial class ChangeApprovedToApprovedBy
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("Relational:MaxIdentifierLength", 63)
                .HasAnnotation("ProductVersion", "5.0.4")
                .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            modelBuilder.Entity("ApplicationUserDbTile", b =>
                {
                    b.Property<Guid>("TilesId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("UsersId")
                        .HasColumnType("uuid");

                    b.HasKey("TilesId", "UsersId");

                    b.HasIndex("UsersId");

                    b.ToTable("ApplicationUserDbTile");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetRoleClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("integer")
                        .HasAnnotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

                    b.Property<string>("ClaimType")
                        .HasColumnType("text");

                    b.Property<string>("ClaimValue")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserClaims");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("ProviderKey")
                        .HasColumnType("text");

                    b.Property<string>("ProviderDisplayName")
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("LoginProvider", "ProviderKey");

                    b.HasIndex("UserId");

                    b.ToTable("AspNetUserLogins");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<Guid>("RoleId")
                        .HasColumnType("uuid");

                    b.HasKey("UserId", "RoleId");

                    b.HasIndex("RoleId");

                    b.ToTable("AspNetUserRoles");
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.Property<string>("LoginProvider")
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("UserId", "LoginProvider", "Name");

                    b.ToTable("AspNetUserTokens");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.ApplicationRole", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Name")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedName")
                        .IsUnique()
                        .HasDatabaseName("RoleNameIndex");

                    b.ToTable("AspNetRoles");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.ApplicationUser", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("AccessFailedCount")
                        .HasColumnType("integer");

                    b.Property<string>("ConcurrencyStamp")
                        .IsConcurrencyToken()
                        .HasColumnType("text");

                    b.Property<string>("Email")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<bool>("EmailConfirmed")
                        .HasColumnType("boolean");

                    b.Property<bool>("LockoutEnabled")
                        .HasColumnType("boolean");

                    b.Property<DateTimeOffset?>("LockoutEnd")
                        .HasColumnType("timestamp with time zone");

                    b.Property<string>("NormalizedEmail")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("NormalizedUserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.Property<string>("PasswordHash")
                        .HasColumnType("text");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("text");

                    b.Property<bool>("PhoneNumberConfirmed")
                        .HasColumnType("boolean");

                    b.Property<string>("SecurityStamp")
                        .HasColumnType("text");

                    b.Property<bool>("TwoFactorEnabled")
                        .HasColumnType("boolean");

                    b.Property<string>("UserName")
                        .HasMaxLength(256)
                        .HasColumnType("character varying(256)");

                    b.HasKey("Id");

                    b.HasIndex("NormalizedEmail")
                        .HasDatabaseName("EmailIndex");

                    b.HasIndex("NormalizedUserName")
                        .IsUnique()
                        .HasDatabaseName("UserNameIndex");

                    b.ToTable("AspNetUsers");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbNote", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<double>("Lat")
                        .HasColumnType("double precision");

                    b.Property<double>("Lon")
                        .HasColumnType("double precision");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<Guid>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("UserId");

                    b.ToTable("Notes");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbStop", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<double>("Lat")
                        .HasColumnType("double precision");

                    b.Property<double>("Lon")
                        .HasColumnType("double precision");

                    b.Property<string>("Name")
                        .HasColumnType("text");

                    b.Property<string>("Number")
                        .HasColumnType("text");

                    b.Property<bool>("OutsideSelectedTile")
                        .HasColumnType("boolean");

                    b.Property<int>("ProviderType")
                        .HasColumnType("integer");

                    b.Property<long>("Ref")
                        .HasColumnType("bigint");

                    b.Property<long>("StopId")
                        .HasColumnType("bigint");

                    b.Property<int>("StopType")
                        .HasColumnType("integer");

                    b.Property<List<Tag>>("Tags")
                        .HasColumnType("jsonb");

                    b.Property<Guid>("TileId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("TileId");

                    b.ToTable("Stops");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbStopLink", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<Guid?>("ApprovedById")
                        .HasColumnType("uuid");

                    b.Property<DateTime>("CreatedAt")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("timestamp without time zone")
                        .HasDefaultValueSql("NOW()");

                    b.Property<Guid>("GtfsStopId")
                        .HasColumnType("uuid");

                    b.Property<bool>("Imported")
                        .HasColumnType("boolean");

                    b.Property<int>("OperationType")
                        .HasColumnType("integer");

                    b.Property<Guid>("OsmStopId")
                        .HasColumnType("uuid");

                    b.Property<DateTime?>("UpdatedAt")
                        .HasColumnType("timestamp without time zone");

                    b.Property<Guid?>("UserId")
                        .HasColumnType("uuid");

                    b.HasKey("Id");

                    b.HasIndex("ApprovedById");

                    b.HasIndex("GtfsStopId");

                    b.HasIndex("OsmStopId");

                    b.HasIndex("UserId");

                    b.ToTable("StopLinks");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbTile", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<int>("GtfsStopsCount")
                        .HasColumnType("integer");

                    b.Property<double>("MaxLat")
                        .HasColumnType("double precision");

                    b.Property<double>("MaxLon")
                        .HasColumnType("double precision");

                    b.Property<double>("MinLat")
                        .HasColumnType("double precision");

                    b.Property<double>("MinLon")
                        .HasColumnType("double precision");

                    b.Property<int>("OsmStopsCount")
                        .HasColumnType("integer");

                    b.Property<double>("OverlapMaxLat")
                        .HasColumnType("double precision");

                    b.Property<double>("OverlapMaxLon")
                        .HasColumnType("double precision");

                    b.Property<double>("OverlapMinLat")
                        .HasColumnType("double precision");

                    b.Property<double>("OverlapMinLon")
                        .HasColumnType("double precision");

                    b.Property<long>("X")
                        .HasColumnType("bigint");

                    b.Property<long>("Y")
                        .HasColumnType("bigint");

                    b.HasKey("Id");

                    b.ToTable("Tiles");
                });

            modelBuilder.Entity("ApplicationUserDbTile", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.DbTile", null)
                        .WithMany()
                        .HasForeignKey("TilesId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UsersId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityRoleClaim<System.Guid>", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.ApplicationRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserClaim<System.Guid>", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserLogin<System.Guid>", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserRole<System.Guid>", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.ApplicationRole", null)
                        .WithMany()
                        .HasForeignKey("RoleId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("Microsoft.AspNetCore.Identity.IdentityUserToken<System.Guid>", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", null)
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbNote", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbStop", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.DbTile", "Tile")
                        .WithMany("Stops")
                        .HasForeignKey("TileId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tile");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbStopLink", b =>
                {
                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", "ApprovedBy")
                        .WithMany()
                        .HasForeignKey("ApprovedById");

                    b.HasOne("OsmIntegrator.Database.Models.DbStop", "GtfsStop")
                        .WithMany()
                        .HasForeignKey("GtfsStopId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("OsmIntegrator.Database.Models.DbStop", "OsmStop")
                        .WithMany("StopLinks")
                        .HasForeignKey("OsmStopId")
                        .OnDelete(DeleteBehavior.NoAction)
                        .IsRequired();

                    b.HasOne("OsmIntegrator.Database.Models.ApplicationUser", "User")
                        .WithMany()
                        .HasForeignKey("UserId");

                    b.Navigation("ApprovedBy");

                    b.Navigation("GtfsStop");

                    b.Navigation("OsmStop");

                    b.Navigation("User");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbStop", b =>
                {
                    b.Navigation("StopLinks");
                });

            modelBuilder.Entity("OsmIntegrator.Database.Models.DbTile", b =>
                {
                    b.Navigation("Stops");
                });
#pragma warning restore 612, 618
        }
    }
}
