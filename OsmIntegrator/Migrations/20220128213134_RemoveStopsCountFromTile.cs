using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class RemoveStopsCountFromTile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GtfsStopsCount",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "OsmStopsCount",
                table: "Tiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GtfsStopsCount",
                table: "Tiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OsmStopsCount",
                table: "Tiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
