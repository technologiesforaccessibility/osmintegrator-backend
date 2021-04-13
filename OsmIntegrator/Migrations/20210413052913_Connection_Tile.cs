using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class Connection_Tile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

        protected override void Down(MigrationBuilder migrationBuilder)
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
        }
    }
}
