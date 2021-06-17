using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace OsmIntegrator.Database.Models
{
    [Table("Versions")]
    public class DbVersion
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public long Id { get; set; }

        public string Description { get; set; }

        [Required]
        public ApplicationUser User { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public long BranchId { get; set; }

        [Required]
        public DbBranch Branch { get; set; }
    }
}