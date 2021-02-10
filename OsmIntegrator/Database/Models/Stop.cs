using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.Database.Models
{
    public class Stop
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        [Required]
        public long StopId { get; set; }

        public string Name { get; set; }

        [Required]
        public double Lat { get; set; }

        [Required]
        public double Lon { get; set; }

        public string Number { get; set; }

        public List<Tag> Tags { get; set; }

        [Required]
        public StopType StopType { get; set; }

        [Required]
        public ProviderType ProviderType { get; set; }

        public Guid TileId { get; set; }

        public Tile Tile { get; set; }
    }
}
