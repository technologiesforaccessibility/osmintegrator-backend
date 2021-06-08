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
    [Table("Fields")]
    public class DbField
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public DbCategory Category { get; set; }

        public FieldType FieldType { get; set; }

        public string Name { get; set; }

        /// <summary>
        /// What is it for?
        /// </summary>
        public string Label { get; set; }

        public string Description { get; set; }
    }
}