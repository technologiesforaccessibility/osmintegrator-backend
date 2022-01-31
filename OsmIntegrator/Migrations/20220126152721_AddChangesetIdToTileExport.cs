using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class AddChangesetIdToTileExport : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "ChangesetId",
                table: "TileExportReports",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChangesetId",
                table: "TileExportReports");
        }
    }
}
