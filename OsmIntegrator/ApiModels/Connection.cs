using System;
using System.ComponentModel.DataAnnotations;
using OsmIntegrator.Enums;

namespace OsmIntegrator.ApiModels
{
    public class Connection
    {
        [Required]
        public Guid GtfsStopId { get; set; }

        [Required]
        public Guid OsmStopId { get; set; }

        [Required]
        public bool Imported { get; set; }
    }
}