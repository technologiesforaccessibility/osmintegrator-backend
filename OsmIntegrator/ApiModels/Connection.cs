using System;
using System.ComponentModel.DataAnnotations;
using OsmIntegrator.Enums;

namespace OsmIntegrator.ApiModels
{
    public class Connection
    {
        [Required]
        public Guid GtfsId { get; set; }

        [Required]
        public Guid OsmId { get; set; }

        [Required]
        public bool Existing { get; set; }
    }
}