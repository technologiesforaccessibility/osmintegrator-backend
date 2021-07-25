using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class NoteStatus : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Approved",
                table: "Notes");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Notes",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "Notes");

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "Notes",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
