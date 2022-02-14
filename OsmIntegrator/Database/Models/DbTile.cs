using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Localization;
using OsmIntegrator.Database.Models.Enums;
using OsmIntegrator.Database.Models.JsonFields;
using OsmIntegrator.Enums;
using OsmIntegrator.Extensions;

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
    public double OverlapMaxLon { get; set; }

    public List<DbStop> Stops { get; set; }

    public List<DbConversation> Conversations { get; set; }

    public List<DbTileImportReport> ChangeReports { get; set; } = new();

    public List<DbTileExportReport> ExportReports { get; set; } = new();

    public DateTime? LastExportAt => ExportReports.Any() ? ExportReports.Select(r => r.CreatedAt).Max() : null;

    public string GetUnexportedChanges(IStringLocalizer localizer)
    {
      IReadOnlyCollection<DbConnection> connections = GetUnexportedOsmConnections();

      StringBuilder sb = new();
      sb.AppendFormat(localizer["Tile coordinates"], X, Y);
      sb.AppendLine(string.Empty);

      if (!connections.Any())
      {
        sb.AppendLine(string.Empty);
        sb.AppendLine(localizer["No changes"]);
        return sb.ToString();
      }

      foreach (var osmStopGroup in connections.GroupBy(c => c.OsmStop).OrderBy(s => s.Key.Name))
      {
        DbStop osmStop = osmStopGroup.Key;
        DbConnection connection = osmStopGroup.FirstOrDefault(s => s.GtfsStop != null);
        string stopDiff = GetStopDiff(osmStop, connection, localizer);

        if (!string.IsNullOrWhiteSpace(stopDiff))
        {
          sb.AppendLine(string.Empty);
          sb.AppendLine($"{localizer["Stop modified"]} {osmStopGroup.Key.Name}, Ver: {osmStopGroup.Key.Version}");
          sb.Append(stopDiff);
        }
      }

      return sb.ToString();
    }

    private static string GetStopDiff(DbStop osmStop, DbConnection connection, IStringLocalizer localizer)
    {
      Tag oldRefTag = osmStop.GetTag(Constants.REF);
      Tag oldLocalRefTag = osmStop.GetTag(Constants.LOCAL_REF);
      Tag oldNameTag = osmStop.GetTag(Constants.NAME);

      string newRef = connection.GtfsStop.StopId.ToString();
      string newLocalRef = connection.GtfsStop.Number;
      string newName = connection.GtfsStop.Name;

      string refTagDiff = GetTagDiff(connection.OperationType, Constants.REF, oldRefTag?.Value, newRef, localizer);
      string localRefTagDiff = GetTagDiff(connection.OperationType, Constants.LOCAL_REF, oldLocalRefTag?.Value, newLocalRef, localizer);
      string nameTagDiff = GetTagDiff(connection.OperationType, Constants.NAME, oldNameTag?.Value, newName, localizer);

      StringBuilder stopDiffBuilder = new();
      stopDiffBuilder.Append(refTagDiff);
      stopDiffBuilder.Append(localRefTagDiff);
      stopDiffBuilder.Append(nameTagDiff);

      return stopDiffBuilder.ToString();
    }

    private static string GetTagDiff(ConnectionOperationType operationType, string tagName, string oldValue, string newValue, IStringLocalizer localizer)
    {
      StringBuilder stopDiffBuilder = new();

      if (operationType == ConnectionOperationType.Added)
      {
        if (string.IsNullOrWhiteSpace(oldValue))
        {
          stopDiffBuilder.AppendLine($"\t{localizer["Tag added"]} {tagName}: {newValue}");
        }
        else if (oldValue != newValue)
        {
          stopDiffBuilder.AppendLine($"\t{localizer["Tag modified"]} {tagName}: {oldValue} => {newValue}");
        }
      }

      return stopDiffBuilder.ToString();
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

    public int GtfsStopsCount => Stops.Count(s => s.StopType == StopType.Gtfs);

    public bool HasNewGtfsConnections => Stops
      .Where(s => s.StopType == StopType.Gtfs)
      .SelectMany(s => s.GtfsConnections)
      .OnlyActive().Any();

    public bool IsAccessibleBy(Guid userId) =>
      Stops.Where(s => s.StopType == StopType.Gtfs)
        .SelectMany(s => s.GtfsConnections)
        .OnlyActive()
        .All(c => c.UserId == userId);

    public IReadOnlyCollection<DbConnection> GetUnexportedOsmConnections() => Stops
      .Where(s => s.StopType == StopType.Osm)
      .SelectMany(s => s.OsmConnections)
      .OnlyActive()
      .Where(c => !c.Exported)
      .ToList();

    public ApplicationUser AssignedUser => Stops
      .Where(s => s.StopType == StopType.Gtfs)
      .SelectMany(s => s.GtfsConnections)
      .OnlyActive()
      .Select(c => c.User)
      .FirstOrDefault();

    public int UnconnectedGtfsStopsCount => Stops
      .Where(s => s.StopType == StopType.Gtfs)
      .Count(s => !s.GtfsConnections.OnlyActive().Any());
  }
}
