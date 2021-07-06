using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class ConnectionsReplaced : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.CreateTable(
                name: "StopLinks",
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
                    table.PrimaryKey("PK_StopLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopLinks_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StopLinks_Stops_GtfsStopId",
                        column: x => x.GtfsStopId,
                        principalTable: "Stops",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StopLinks_Stops_OsmStopId",
                        column: x => x.OsmStopId,
                        principalTable: "Stops",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_StopLinks_GtfsStopId",
                table: "StopLinks",
                column: "GtfsStopId");

            migrationBuilder.CreateIndex(
                name: "IX_StopLinks_OsmStopId",
                table: "StopLinks",
                column: "OsmStopId");

            migrationBuilder.CreateIndex(
                name: "IX_StopLinks_UserId",
                table: "StopLinks",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StopLinks");

            migrationBuilder.CreateTable(
                name: "Connections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    GtfsStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    Imported = table.Column<bool>(type: "boolean", nullable: false),
                    OperationType = table.Column<int>(type: "integer", nullable: false),
                    OsmStopId = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true)
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
        }
    }
}
