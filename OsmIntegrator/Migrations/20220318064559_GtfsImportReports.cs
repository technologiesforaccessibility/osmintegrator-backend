using System;
using Microsoft.EntityFrameworkCore.Migrations;
using OsmIntegrator.Database.Models.JsonFields;

#nullable disable

namespace osmintegrator.Migrations
{
    public partial class GtfsImportReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GtfsImportReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GtfsReport = table.Column<GtfsImportReport>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GtfsImportReports", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GtfsImportReports");
        }
    }
}
