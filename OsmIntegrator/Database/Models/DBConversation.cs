using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsmIntegrator.Database.Models
{
  [Table("Conversations")]
  public class DbConversation
  {
    [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }

    public double? Lat { get; set; }

    public double? Lon { get; set; }

    public Guid? StopId { get; set; }

    public DbStop Stop { get; set; }

    [Required]
    public Guid TileId { get; set; }

    [Required]
    public DbTile Tile { get; set; }

    public List<DbMessage> Messages { get; set; }
  }
}
