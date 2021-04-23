using System;
using System.ComponentModel.DataAnnotations;
using OsmIntegrator.Enums;

namespace OsmIntegrator.ApiModels
{
    public class ConnectionAction
    {
        [Required]
        public Guid OsmStopId { get; set; }

        [Required]
        public Guid GtfsStopId { get; set; }
    }
}