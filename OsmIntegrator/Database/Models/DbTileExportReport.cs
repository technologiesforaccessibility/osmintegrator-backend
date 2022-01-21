using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Database.Models;
using OsmIntegrator.Database.Models.JsonFields;

namespace OsmIntegrator.Database.Models
{
  [Table("TileExportReport")]
  public class DbTileExportReport
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public TileExportReport TileReport { get; set; }

    public Guid TileId { get; set; }
    public DbTile Tile { get; set; }
  }
}