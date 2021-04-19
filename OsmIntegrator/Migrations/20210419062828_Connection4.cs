using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class Connection4 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Connections",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "Removed",
                table: "Connections");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "Connections",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "OperationType",
                table: "Connections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Connections",
                table: "Connections",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Connections_OsmStopId",
                table: "Connections",
                column: "OsmStopId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Connections",
                table: "Connections");

            migrationBuilder.DropIndex(
                name: "IX_Connections_OsmStopId",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "OperationType",
                table: "Connections");

            migrationBuilder.AddColumn<bool>(
                name: "Removed",
                table: "Connections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Connections",
                table: "Connections",
                columns: new[] { "OsmStopId", "GtfsStopId", "Imported" });
        }
    }
}
