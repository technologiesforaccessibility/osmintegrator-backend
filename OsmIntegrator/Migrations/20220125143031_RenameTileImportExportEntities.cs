using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
  public partial class RenameTileImportExportEntities : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.RenameTable(
          name: "TileExportReport",
          newName: "TileExportReports");

      migrationBuilder.RenameTable(
          name: "ChangeReports",
          newName: "TileImportReports");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.RenameTable(
          name: "TileExportReports",
          newName: "TileExportReport");

      migrationBuilder.RenameTable(
          name: "TileImportReports",
          newName: "ChangeReports");
    }
  }
}
