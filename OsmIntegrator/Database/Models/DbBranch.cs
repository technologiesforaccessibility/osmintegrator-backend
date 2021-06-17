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
    [Table("Branches")]
    public class DbBranch
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public string Name { get; set; }
    }
}