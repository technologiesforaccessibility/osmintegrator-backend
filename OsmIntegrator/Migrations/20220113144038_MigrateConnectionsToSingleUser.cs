using System.IO;
using Microsoft.EntityFrameworkCore.Migrations;

namespace osmintegrator.Migrations
{
  public partial class MigrateConnectionsToSingleUser : Migration
  {
    protected override void Up(MigrationBuilder migrationBuilder)
    {
      var sql = File.ReadAllText("./Scripts/MigrateConnectionsToSingleUser.sql");
      migrationBuilder.Sql(sql);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {

    }
  }
}
