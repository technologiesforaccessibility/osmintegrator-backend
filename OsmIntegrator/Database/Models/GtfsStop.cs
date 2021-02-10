using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace OsmIntegrator.Database.Models
{
    public class GtfsStop
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public long StopId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Number { get; set; }

        [Required]
        public double Lat { get; set; }

        [Required]
        public double Lon { get; set; }
        
    }
}
