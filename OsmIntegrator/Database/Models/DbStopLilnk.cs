using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using OsmIntegrator.Enums;

namespace OsmIntegrator.Database.Models
{
    [Table("StopLinks")]
    public class DbStopLink
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public Guid Id { get; set; }

        public Guid OsmStopId { get; set; }

        [Required]
        public DbStop OsmStop { get; set; }

        public Guid GtfsStopId { get; set; }

        [Required]
        public DbStop GtfsStop { get; set; }

        [Required]
        public bool Imported { get; set; }

        public Guid? UserId { get; set; }

        public ApplicationUser User { get; set; }

        public ConnectionOperationType OperationType { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
        
        public ApplicationUser ApprovedBy {get; set;}
    }

    public class DbStopLinkComparer : IEqualityComparer<DbStopLink>
    {
        /* 
            This lives here temporairly for simplicity. Dependency rule broken on purpose.
        */
        public bool Equals(DbStopLink x, DbStopLink y)
        {
            if (Object.ReferenceEquals(x, y)) return true;
            if (Object.ReferenceEquals(x, null) || Object.ReferenceEquals(y, null))
                return false;
            
            return x.OsmStopId == y.OsmStopId && x.GtfsStopId == y.GtfsStopId;
            
        }

        public int GetHashCode([DisallowNull] DbStopLink obj)
        {            
            if (Object.ReferenceEquals(obj, null)) return 0;

            int hashProductName = obj.OsmStopId == null ? 0 : obj.OsmStopId.GetHashCode();            
            int hashProductCode = obj.GtfsStopId == null ? 0 : obj.GtfsStopId.GetHashCode();

            return hashProductName ^ hashProductCode;
        }
    }
}