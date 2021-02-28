﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

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

        public List<DbTag> Tags { get; set; }

        [Required]
        public StopType StopType { get; set; }

        [Required]
        public ProviderType ProviderType { get; set; }

        public Guid TileId { get; set; }

        public DbTile Tile { get; set; }

        public bool OutsideSelectedTile { get; set; } = false;
    }
}