using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class RemovedTileUnusedFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tiles_AspNetUsers_EditorApprovedId",
                table: "Tiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Tiles_AspNetUsers_SupervisorApprovedId",
                table: "Tiles");

            migrationBuilder.DropIndex(
                name: "IX_Tiles_EditorApprovedId",
                table: "Tiles");

            migrationBuilder.DropIndex(
                name: "IX_Tiles_SupervisorApprovedId",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "EditorApprovalTime",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "EditorApprovedId",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovalTime",
                table: "Tiles");

            migrationBuilder.DropColumn(
                name: "SupervisorApprovedId",
                table: "Tiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EditorApprovalTime",
                table: "Tiles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "EditorApprovedId",
                table: "Tiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SupervisorApprovalTime",
                table: "Tiles",
                type: "timestamp without time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SupervisorApprovedId",
                table: "Tiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_EditorApprovedId",
                table: "Tiles",
                column: "EditorApprovedId");

            migrationBuilder.CreateIndex(
                name: "IX_Tiles_SupervisorApprovedId",
                table: "Tiles",
                column: "SupervisorApprovedId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tiles_AspNetUsers_EditorApprovedId",
                table: "Tiles",
                column: "EditorApprovedId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Tiles_AspNetUsers_SupervisorApprovedId",
                table: "Tiles",
                column: "SupervisorApprovedId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
