using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsmIntegrator.Database.Models
{

  [Table("TileUser")]
  public class DbTileUser
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key] 
    public Guid Id { get; set; }
    public DbTile Tile { get; set; }

    public ApplicationUser User { get; set; }

    public ApplicationRole Role { get; set; }
  }
}