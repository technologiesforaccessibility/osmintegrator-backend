using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class ApprovingNotes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StopLinks");

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "Notes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "ApproverId",
                table: "Notes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TileId",
                table: "Notes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

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
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()"),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Connections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Connections_AspNetUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_Notes_ApproverId",
                table: "Notes",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_TileId",
                table: "Notes",
                column: "TileId");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_ApprovedById",
                table: "Connections",
                column: "ApprovedById");

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
                name: "FK_Notes_AspNetUsers_ApproverId",
                table: "Notes",
                column: "ApproverId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Notes_Tiles_TileId",
                table: "Notes",
                column: "TileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notes_AspNetUsers_ApproverId",
                table: "Notes");

            migrationBuilder.DropForeignKey(
                name: "FK_Notes_Tiles_TileId",
                table: "Notes");

            migrationBuilder.DropTable(
                name: "Connections");

            migrationBuilder.DropIndex(
                name: "IX_Notes_ApproverId",
                table: "Notes");

            migrationBuilder.DropIndex(
                name: "IX_Notes_TileId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "ApproverId",
                table: "Notes");

            migrationBuilder.DropColumn(
                name: "TileId",
                table: "Notes");

            migrationBuilder.CreateTable(
                name: "StopLinks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedById = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_StopLinks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StopLinks_AspNetUsers_ApprovedById",
                        column: x => x.ApprovedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                name: "IX_StopLinks_ApprovedById",
                table: "StopLinks",
                column: "ApprovedById");

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
    }
}
