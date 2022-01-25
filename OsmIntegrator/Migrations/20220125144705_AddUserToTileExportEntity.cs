using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class AddUserToTileExportEntity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "TileExportReports",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_TileExportReports_UserId",
                table: "TileExportReports",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_TileExportReports_AspNetUsers_UserId",
                table: "TileExportReports",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TileExportReports_AspNetUsers_UserId",
                table: "TileExportReports");

            migrationBuilder.DropIndex(
                name: "IX_TileExportReports_UserId",
                table: "TileExportReports");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "TileExportReports");
        }
    }
}
