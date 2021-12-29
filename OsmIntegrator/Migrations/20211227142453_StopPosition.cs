using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class StopPosition : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "InitLat",
                table: "Stops",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "InitLon",
                table: "Stops",
                type: "double precision",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InitLat",
                table: "Stops");

            migrationBuilder.DropColumn(
                name: "InitLon",
                table: "Stops");
        }
    }
}
