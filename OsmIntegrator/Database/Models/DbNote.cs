using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.Database.Models
{
    [Table("Notes")]
    public class DbNote
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        [Required]
        public string Text { get; set; }

        [Required]
        public double Lat { get; set; }

        [Required]
        public double Lon { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public ApplicationUser User { get; set; }

        public Guid? ApproverId { get; set; }

        public bool Approved { get; set; }

        public ApplicationUser Approver { get; set; }

        [Required]
        public Guid TileId { get; set; }

        [Required]
        public DbTile Tile { get; set; }
    }
}
