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
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OsmStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    GtfsStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Imported = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Connections_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.CreateIndex(
                name: "IX_Connections_OsmStopId",
                table: "Connections",
                column: "OsmStopId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_UserId",
                table: "Connections",
                column: "UserId");

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
