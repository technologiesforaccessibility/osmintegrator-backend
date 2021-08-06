using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OsmIntegrator.Database.Models
{
    [Table("Stops")]
    public class DbStop
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

        [Column(TypeName = "jsonb")]
        public List<Tag> Tags { get; set; }

        [Required]
        public StopType StopType { get; set; }

        [Required]
        public ProviderType ProviderType { get; set; }

        public Guid TileId { get; set; }

        public DbTile Tile { get; set; }

        public bool OutsideSelectedTile { get; set; } = false;

        public List<DbConnections> GtfsConnections { get; set; }

        public List<DbConnections> OsmConnections { get; set; }

        public long Ref { get; set; }

        public int Version { get; set; }

        public string Changeset { get; set; }
    }
}
