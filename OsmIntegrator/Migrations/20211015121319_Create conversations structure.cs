using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
  public partial class Createconversationsstructure : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.CreateTable(
          name: "Conversations",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Lat = table.Column<double>(type: "double precision", nullable: true),
            Lon = table.Column<double>(type: "double precision", nullable: true),
            StopId = table.Column<Guid>(type: "uuid", nullable: true),
            TileId = table.Column<Guid>(type: "uuid", nullable: false)
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Conversations", x => x.Id);
            table.ForeignKey(
                      name: "FK_Conversations_Stops_StopId",
                      column: x => x.StopId,
                      principalTable: "Stops",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Restrict);
            table.ForeignKey(
                      name: "FK_Conversations_Tiles_TileId",
                      column: x => x.TileId,
                      principalTable: "Tiles",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateTable(
          name: "Messages",
          columns: table => new
          {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
            Text = table.Column<string>(type: "text", nullable: true),
            UserId = table.Column<Guid>(type: "uuid", nullable: false),
            Status = table.Column<int>(type: "integer", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "NOW()")
          },
          constraints: table =>
          {
            table.PrimaryKey("PK_Messages", x => x.Id);
            table.ForeignKey(
                      name: "FK_Messages_AspNetUsers_UserId",
                      column: x => x.UserId,
                      principalTable: "AspNetUsers",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
            table.ForeignKey(
                      name: "FK_Messages_Conversations_ConversationId",
                      column: x => x.ConversationId,
                      principalTable: "Conversations",
                      principalColumn: "Id",
                      onDelete: ReferentialAction.Cascade);
          });

      migrationBuilder.CreateIndex(
          name: "IX_Conversations_StopId",
          table: "Conversations",
          column: "StopId");

      migrationBuilder.CreateIndex(
          name: "IX_Conversations_TileId",
          table: "Conversations",
          column: "TileId");

      migrationBuilder.CreateIndex(
          name: "IX_Messages_ConversationId",
          table: "Messages",
          column: "ConversationId");

      migrationBuilder.CreateIndex(
          name: "IX_Messages_UserId",
          table: "Messages",
          column: "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
      migrationBuilder.DropTable(
          name: "Messages");

      migrationBuilder.DropTable(
          name: "Conversations");
    }
  }
}
