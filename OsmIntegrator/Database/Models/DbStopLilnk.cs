using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using OsmIntegrator.Enums;

namespace OsmIntegrator.Database.Models
{
    [Table("StopLinks")]
    public class DbStopLink
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        public Guid OsmStopId { get; set; }

        [Required]
        public DbStop OsmStop { get; set; }

        public Guid GtfsStopId { get; set; }

        [Required]
        public DbStop GtfsStop { get; set; }

        [Required]
        public bool Imported { get; set; }

        public Guid? UserId { get; set; }

        public ApplicationUser User { get; set; }

        public ConnectionOperationType OperationType { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
        
        public bool Approved {get; set;}
    }
}