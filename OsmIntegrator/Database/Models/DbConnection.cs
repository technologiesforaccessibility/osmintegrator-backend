using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsmIntegrator.Database.Models
{
    [Table("Connections")]
    public class DbConnection
    {
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
    }
}