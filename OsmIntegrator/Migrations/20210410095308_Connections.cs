using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class Connections : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Stops_OsmStopId",
                table: "Tags");

            migrationBuilder.RenameColumn(
                name: "OsmStopId",
                table: "Tags",
                newName: "StopId");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_OsmStopId",
                table: "Tags",
                newName: "IX_Tags_StopId");

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    OsmStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    GtfsStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Existing = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => new { x.OsmStopId, x.GtfsStopId });
                    table.ForeignKey(
                        name: "FK_Connections_Stops_GtfsStopId",
                        column: x => x.GtfsStopId,
                        principalTable: "Stops",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Connections_Stops_OsmStopId",
                        column: x => x.OsmStopId,
                        principalTable: "Stops",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Connections_GtfsStopId",
                table: "Connections",
                column: "GtfsStopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Stops_StopId",
                table: "Tags",
                column: "StopId",
                principalTable: "Stops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_Stops_StopId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.RenameColumn(
                name: "StopId",
                table: "Tags",
                newName: "OsmStopId");

            migrationBuilder.RenameIndex(
                name: "IX_Tags_StopId",
                table: "Tags",
                newName: "IX_Tags_OsmStopId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_Stops_OsmStopId",
                table: "Tags",
                column: "OsmStopId",
                principalTable: "Stops",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
