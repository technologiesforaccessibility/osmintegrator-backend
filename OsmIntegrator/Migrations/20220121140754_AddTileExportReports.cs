using System;
using Microsoft.EntityFrameworkCore.Migrations;
using OsmIntegrator.Database.Models.JsonFields;

namespace osmintegrator.Migrations
{
    public partial class AddTileExportReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TileExportReport",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TileReport = table.Column<TileExportReport>(type: "jsonb", nullable: false),
                    TileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TileExportReport", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TileExportReport_Tiles_TileId",
                        column: x => x.TileId,
                        principalTable: "Tiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TileExportReport_TileId",
                table: "TileExportReport",
                column: "TileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TileExportReport");
        }
    }
}
