using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class TileApproval : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TileApprover");

            migrationBuilder.AddColumn<DateTime>(
                name: "EditorApprovalTime",
                table: "Tiles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EditorApprovedId",
                table: "Tiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SupervisorApprovalTime",
                table: "Tiles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupervisorApprovedId",
                table: "Tiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_EditorApprovedId",
                table: "Tiles",
                column: "EditorApprovedId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_SupervisorApprovedId",
                table: "Tiles",
                column: "SupervisorApprovedId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tiles_AspNetUsers_EditorApprovedId",
                table: "Tiles",
                column: "EditorApprovedId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tiles_AspNetUsers_SupervisorApprovedId",
                table: "Tiles",
                column: "SupervisorApprovedId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tiles_AspNetUsers_EditorApprovedId",
                table: "Tiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Tiles_AspNetUsers_SupervisorApprovedId",
                table: "Tiles");

            migrationBuilder.DropIndex(
                name: "IX_Tiles_EditorApprovedId",
                table: "Tiles");

            migrationBuilder.DropIndex(
                name: "IX_Tiles_SupervisorApprovedId",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "EditorApprovalTime",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "EditorApprovedId",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovalTime",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovedId",
                table: "Tiles");

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
    }
}
