using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class AddApproversToTile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TileApprover",
                columns: table => new
                {
                    ApprovedTilesId = table.Column<Guid>(type: "uuid", nullable: false),
                    ApproversId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TileApprover", x => new { x.ApprovedTilesId, x.ApproversId });
                    table.ForeignKey(
                        name: "FK_TileApprover_AspNetUsers_ApproversId",
                        column: x => x.ApproversId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TileApprover_Tiles_ApprovedTilesId",
                        column: x => x.ApprovedTilesId,
                        principalTable: "Tiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TileApprover_ApproversId",
                table: "TileApprover",
                column: "ApproversId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TileApprover");
        }
    }
}
