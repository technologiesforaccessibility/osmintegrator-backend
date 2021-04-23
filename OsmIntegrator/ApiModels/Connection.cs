using System;
using System.ComponentModel.DataAnnotations;
using OsmIntegrator.Enums;

namespace OsmIntegrator.ApiModels
{
    public class Connection
    {
        public Guid GtfsStopId { get; set; }

        public Guid OsmStopId { get; set; }

        public bool Imported { get; set; }
    }
}