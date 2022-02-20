using System;
using Microsoft.EntityFrameworkCore.Migrations;
using OsmIntegrator.Database.Models.JsonFields;

namespace osmintegrator.Migrations
{
    public partial class ChangeReports : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Ref",
                table: "Stops",
                type: "text",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.CreateTable(
                name: "ChangeReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    TileReport = table.Column<TileImportReport>(type: "jsonb", nullable: false),
                    TileId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChangeReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChangeReports_Tiles_TileId",
                        column: x => x.TileId,
                        principalTable: "Tiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChangeReports_TileId",
                table: "ChangeReports",
                column: "TileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChangeReports");

            migrationBuilder.AlterColumn<long>(
                name: "Ref",
                table: "Stops",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);
        }
    }
}
