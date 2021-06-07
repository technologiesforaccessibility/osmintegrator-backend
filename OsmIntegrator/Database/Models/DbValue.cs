using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database.Models
{
    [Table("Values")]
    public class DbValue
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public DbObject Object { get; set; }

        public DbField Field { get; set; }

        public DbVersion Version { get; set; }

        public DbObject RelatedObject { get; set; }

        public int? IntValue { get; set; }

        public double? DoubleValue { get; set; }

        public string StringValue { get; set; }

        public bool? BooleanValue { get; set; }

        public DateTime? DateTimeValue { get; set; }
    }
}