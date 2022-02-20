using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OsmIntegrator.Database.Models.JsonFields;

namespace OsmIntegrator.Database.Models
{
  [Table("TileImportReports")]
  public class DbTileImportReport
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public TileImportReport TileReport { get; set; }

    public Guid TileId { get; set; }
    
    public DbTile Tile { get; set; }
  }
}