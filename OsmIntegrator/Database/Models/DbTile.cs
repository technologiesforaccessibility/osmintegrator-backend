using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
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

    public List<DbChangeReport> ChangeReports { get; set; } = new();

    public List<DbTileExportReport> ExportReports { get; set; } = new();

    public DateTime? LastExportAt => ExportReports.Any() ? ExportReports.Select(r => r.CreatedAt).Max() : null;

    public string GetUnexportedChanges(IStringLocalizer _localizer)
    {
      var connections = Stops.SelectMany(s => s.GtfsConnections)
        .Where(c => !c.Exported)
        .OnlyLatest()
        .ToList();

      StringBuilder sb = new StringBuilder();
      sb.AppendFormat(_localizer["Tile coordinates"], X, Y);
      sb.AppendLine(string.Empty);

      if (!connections.Any())
      {
        sb.AppendLine(string.Empty);
        sb.AppendLine(_localizer["No changes"]);
        return sb.ToString();
      }

      var addedConnections = connections
        .Where(c => c.OperationType == ConnectionOperationType.Added)
        .ToList();

      if (addedConnections.Any())
      {
        sb.AppendLine(string.Empty);
        sb.AppendLine(_localizer["New connections"]);
        sb.AppendLine(string.Empty);
        foreach (var connection in addedConnections)
        {
          sb.AppendLine($"{connection.GtfsStop.Name} <=> {connection.OsmStop.Name}");
        }
        sb.AppendLine(string.Empty);
      }

      var removedConnections = connections
        .Where(c => c.OperationType == ConnectionOperationType.Removed)
        .ToList();

      if (removedConnections.Any())
      {
        sb.AppendLine(_localizer["Removed connections"]);
        sb.AppendLine(string.Empty);
        foreach (var connection in addedConnections)
        {
          sb.AppendLine($"{connection.GtfsStop.Name} <=> {connection.OsmStop.Name}");
        }
        sb.AppendLine(string.Empty);
      }

      return sb.ToString();
    }

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
      .SelectMany(s => s.GtfsConnections)
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

    public ApplicationUser AssignedUser => Stops
      .Where(s => s.StopType == StopType.Gtfs)
      .SelectMany(s => s.GtfsConnections)
      .OnlyActive()
      .Select(c => c.User)
      .FirstOrDefault(u => u != null);

    public int UnconnectedZtmStops => Stops
      .Where(s => s.StopType == StopType.Gtfs)
      .Where(s => !s.GtfsConnections.OnlyActive().Any())
      .Count();
  }
}
