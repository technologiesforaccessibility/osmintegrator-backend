using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Enums;

namespace OsmIntegrator.Database.Models
{
  [Table("Tiles")]
  public class DbTile
  {
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public Guid Id { get; set; }

    [Required]
    public long X { get; set; }

    [Required]
    public long Y { get; set; }

    [Required]
    public double MaxLat { get; set; }

    [Required]
    public double MinLon { get; set; }

    [Required]
    public double MinLat { get; set; }

    [Required]
    public double MaxLon { get; set; }

    [Required]
    public double OverlapMaxLat { get; set; }

    [Required]
    public double OverlapMinLon { get; set; }

    [Required]
    public double OverlapMinLat { get; set; }

    [Required]
    public double OverlapMaxLon { get; set; } = 0;

    public int OsmStopsCount { get; set; } = 0;

    public int GtfsStopsCount { get; set; }

    public List<DbStop> Stops { get; set; }

    public List<ApplicationUser> Users { get; set; }

    public Guid? EditorApprovedId { get; set; }

    public ApplicationUser EditorApproved { get; set; }

    public DateTime? EditorApprovalTime { get; set; }

    public Guid? SupervisorApprovedId { get; set; }

    public ApplicationUser SupervisorApproved { get; set; }

    public DateTime? SupervisorApprovalTime { get; set; }

    public List<DbNote> Notes { get; set; }

    public List<DbConversation> Conversations { get; set; }

    public List<DbTileUser> TileUsers { get; set; }

    public List<DbChangeReport> ChangeReports { get; set; }

    public DbTile()
    {

    }

    public DbTile(long x, long y,
        double minLon, double maxLon, double minLat, double maxLat, double overlapFactor)
    {
      double width = maxLon - minLon;
      double height = maxLat - minLat;
      double lonOverlap = width * overlapFactor;
      double latOverlap = height * overlapFactor;

      Id = Guid.NewGuid();
      X = x;
      Y = y;
      MinLon = minLon;
      MaxLat = maxLat;
      MaxLon = maxLon;
      MinLat = minLat;
      OverlapMinLon = MinLon - lonOverlap;
      OverlapMaxLon = MaxLon + lonOverlap;
      OverlapMinLat = minLat - latOverlap;
      OverlapMaxLat = maxLat + latOverlap;
    }

    public bool HasNewGtfsConnections => Stops
      .Where(s => s.StopType == StopType.Gtfs)
      .SelectMany(s=>s.GtfsConnections)
      .OnlyActive()
      .Any(c => c.UserId.HasValue);

    public bool IsAccessibleBy(Guid userId) => !Stops
      .Where(s => s.StopType == StopType.Gtfs) 
      .SelectMany(s => s.GtfsConnections)
      .OnlyActive()
      .Any(c => c.UserId.HasValue && c.UserId != userId);
    
    public IEnumerable<DbConnection> ActiveConnections(bool exported) => Stops
      .Where(s => s.StopType == StopType.Osm)
      .SelectMany(s => s.OsmConnections)
      .OnlyActive()
      .Where(c => c.Exported == exported);
  }
}
