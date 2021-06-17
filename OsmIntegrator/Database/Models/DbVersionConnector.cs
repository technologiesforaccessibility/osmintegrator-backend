using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Database.Models.Enums;

namespace OsmIntegrator.Database.Models
{
    [Table("VersionConnector")]
    public class DbVersionConnector
    {
        [Required]
        public long ParentId { get; set; }

        [Required]
        public DbVersion Parent { get; set; }

        [Required]
        public long ChildId { get; set; }

        [Required]
        public DbVersion Child { get; set; }
    }
}