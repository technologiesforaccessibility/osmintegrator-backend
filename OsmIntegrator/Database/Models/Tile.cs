using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.Database.Models
{
    public class Tile
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        [Required]
        public long X { get; set; }

        [Required]
        public long Y { get; set; }

        [Required]
        public double Lat { get; set; }

        [Required]
        public double Lon { get; set; }

        public override bool Equals(object obj)
        {
            return obj is Tile tile &&
                   X == tile.X &&
                   Y == tile.Y;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y);
        }
    }
}
