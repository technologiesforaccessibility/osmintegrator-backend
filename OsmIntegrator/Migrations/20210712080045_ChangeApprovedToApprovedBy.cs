using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
    public partial class ChangeApprovedToApprovedBy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Approved",
                table: "StopLinks");

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedById",
                table: "StopLinks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StopLinks_ApprovedById",
                table: "StopLinks",
                column: "ApprovedById");

            migrationBuilder.AddForeignKey(
                name: "FK_StopLinks_AspNetUsers_ApprovedById",
                table: "StopLinks",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StopLinks_AspNetUsers_ApprovedById",
                table: "StopLinks");

            migrationBuilder.DropIndex(
                name: "IX_StopLinks_ApprovedById",
                table: "StopLinks");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "StopLinks");

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "StopLinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
