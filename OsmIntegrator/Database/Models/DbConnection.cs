using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database.Models
{
    [Table("Connections")]
    public class DbConnection
    {
        // [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        // [Key]
        // public Guid Id { get; set; }
        public Guid OsmStopId { get; set; }

        [Required]
        public DbStop OsmStop { get; set; }
        public Guid GtfsStopId { get; set; }

        [Required]
        public DbStop GtfsStop { get; set; }

        [Required]
        public bool Imported { get; set; }

        public Guid TileId { get; set; }

        [Required]
        public DbTile Tile { get; set; }

        public Guid UserId { get; set; }

        public ApplicationUser User { get; set; }
    }
}