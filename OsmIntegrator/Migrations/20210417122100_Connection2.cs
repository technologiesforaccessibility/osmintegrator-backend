using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class Connection2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connections_Tiles_TileId",
                table: "Connections");

            migrationBuilder.DropIndex(
                name: "IX_Connections_TileId",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "TileId",
                table: "Connections");

            migrationBuilder.RenameColumn(
                name: "Existing",
                table: "Connections",
                newName: "Imported");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Connections",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_UserId",
                table: "Connections",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Connections_AspNetUsers_UserId",
                table: "Connections",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connections_AspNetUsers_UserId",
                table: "Connections");

            migrationBuilder.DropIndex(
                name: "IX_Connections_UserId",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Connections");

            migrationBuilder.RenameColumn(
                name: "Imported",
                table: "Connections",
                newName: "Existing");

            migrationBuilder.AddColumn<Guid>(
                name: "TileId",
                table: "Connections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Connections_TileId",
                table: "Connections",
                column: "TileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Connections_Tiles_TileId",
                table: "Connections",
                column: "TileId",
                principalTable: "Tiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
