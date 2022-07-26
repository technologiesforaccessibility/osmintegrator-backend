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

    [Required] public long X { get; set; }

    [Required] public long Y { get; set; }

    [Required] public double MaxLat { get; set; }

    [Required] public double MinLon { get; set; }

    [Required] public double MinLat { get; set; }

    [Required] public double MaxLon { get; set; }

    [Required] public double OverlapMaxLat { get; set; }

    [Required] public double OverlapMinLon { get; set; }

    [Required] public double OverlapMinLat { get; set; }

    [Required] public double OverlapMaxLon { get; set; }

    public List<DbStop> Stops { get; set; }

    public List<DbConversation> Conversations { get; set; }

    public List<DbTileImportReport> ChangeReports { get; set; } = new();

    public List<DbTileExportReport> ExportReports { get; set; } = new();

    public DateTime? LastExportAt => ExportReports.Any() ? ExportReports.Select(r => r.CreatedAt).Max() : null;

    public string GetUnexportedChanges(IStringLocalizer localizer)
    {
      IReadOnlyCollection<DbConnection> connections = GetUnexportedGtfsConnections();

      StringBuilder sb = new();
      sb.AppendFormat(localizer["Tile coordinates"], X, Y);
      sb.AppendLine(string.Empty);

      if (!connections.Any())
      {
        sb.AppendLine(string.Empty);
        sb.AppendLine(localizer["No changes"]);
        return sb.ToString();
      }

      foreach (DbConnection connection in connections)
      {
        DbStop osmStop = connection.OsmStop;
        string stopDiff = GetStopDiff(osmStop, connection, localizer);

        if (string.IsNullOrWhiteSpace(stopDiff)) continue;
        sb.AppendLine(string.Empty);
        sb.AppendLine($"{localizer["Stop modified"]} {osmStop.Name}, Ver: {osmStop.Version}");
        sb.Append(stopDiff);
      }

      return sb.ToString();
    }

    private static string GetStopDiff(DbStop osmStop, DbConnection connection, IStringLocalizer localizer)
    {
      Tag oldNameTag = osmStop.GetTag(Constants.NAME);
      Tag oldRefTag = osmStop.GetTag(Constants.REF);
      Tag oldLocalRefTag = osmStop.GetTag(Constants.LOCAL_REF);
      Tag oldMetropoliaRefTag = osmStop.GetTag(Constants.REF_METROPOLIA);
      
      string newName = connection.GtfsStop.Name;
      string newRef = connection.GtfsStop.Number;
      string newLocalRef = connection.GtfsStop.Number;
      string newMetropoliaRef = connection.GtfsStop.StopId.ToString();
      
      string nameTagDiff = GetTagDiff(Constants.NAME, oldNameTag?.Value, newName, localizer);
      string refTagDiff = GetTagDiff(Constants.REF, oldRefTag?.Value, newRef, localizer);
      string localRefTagDiff = GetTagDiff(Constants.LOCAL_REF, oldLocalRefTag?.Value,
        newLocalRef, localizer);
      string metropoliaRefTagDiff = GetTagDiff(Constants.REF_METROPOLIA, oldMetropoliaRefTag?.Value, 
        newMetropoliaRef, localizer);
      
      StringBuilder stopDiffBuilder = new();
      if (nameTagDiff != null) stopDiffBuilder.AppendLine(nameTagDiff);
      if(refTagDiff != null) stopDiffBuilder.AppendLine(refTagDiff);
      if(localRefTagDiff != null) stopDiffBuilder.AppendLine(localRefTagDiff);
      if (metropoliaRefTagDiff != null) stopDiffBuilder.AppendLine(metropoliaRefTagDiff);

      return stopDiffBuilder.ToString();
    }

    private static string GetTagDiff(string tagName, string oldValue, string newValue, IStringLocalizer localizer)
    {
      if (string.IsNullOrWhiteSpace(oldValue))
      {
        return $"\t{localizer["[TAG-ADDED]"]} {tagName}: {newValue}";
      }
      if (oldValue != newValue)
      {
        return $"\t{localizer["[TAG-MODIFIED]"]} {tagName}: {newValue} ({oldValue})";
      }

      return null;
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

    public IReadOnlyCollection<DbConnection> GetUnexportedGtfsConnections() => Stops
      .Where(s => s.StopType == StopType.Gtfs)
      .SelectMany(s => s.GtfsConnections)
      .OnlyActive()
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