using OsmIntegrator.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace osmintegrator.Database.Models
{
    public class Stop
    {
        [Key]
        public int Id { get; set; }

        public int StopId { get; set; }

        public string Name { get; set; }

        public string Number { get; set; }

        public float Lat { get; set; }

        public float Lon { get; set; }
        
        public StopType StopType { get; set; }
    }
}
