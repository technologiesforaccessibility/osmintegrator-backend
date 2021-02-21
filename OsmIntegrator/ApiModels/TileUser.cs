using System;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class TileUser
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public bool IsAssigned{ get; set; }
    }
}