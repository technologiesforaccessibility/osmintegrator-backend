using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OsmIntegrator.Database.Models.JsonFields;

namespace OsmIntegrator.Database.Models
{
  [Table("TileExportReports")]
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

    [Required]
    public Guid TileId { get; set; }

    [Required]
    public DbTile Tile { get; set; }

    [Required]
    public Guid UserId { get; set; }

    [Required]
    public ApplicationUser User { get; set; }
  }
}