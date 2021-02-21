using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace OsmIntegrator.ApiModels
{
    public class TileWithUsers
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        public List<TileUser> Users { get; set; }
    }
}