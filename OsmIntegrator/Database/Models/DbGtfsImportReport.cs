using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OsmIntegrator.Database.Models.JsonFields;

namespace OsmIntegrator.Database.Models
{
  [Table("GtfsImportReports")]
  public class DbGtfsImportReport
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; }

    [Required]
    [Column(TypeName = "jsonb")]
    public GtfsImportReport GtfsReport { get; set; }
  }
}