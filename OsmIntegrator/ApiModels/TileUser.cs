using System;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class TileUser
    {
        [Required]
        public long Id { get; set; }

        [Required]
        public string UserName {get; set; }

        [Required]
        public bool IsAssigned{ get; set; }
    }
}