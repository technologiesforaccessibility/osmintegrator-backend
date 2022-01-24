using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class ConnectionApprovalRemoved : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Connections_AspNetUsers_ApprovedById",
                table: "Connections");

            migrationBuilder.DropIndex(
                name: "IX_Connections_ApprovedById",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "Imported",
                table: "Connections");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Connections");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "Connections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Imported",
                table: "Connections",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Connections",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Connections_ApprovedById",
                table: "Connections",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Connections_AspNetUsers_ApprovedById",
                table: "Connections",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
