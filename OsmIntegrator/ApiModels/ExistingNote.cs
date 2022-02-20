using System;
using OsmIntegrator.Database.Models.Enums;

public class ExistingNote
{
    public Guid Id { get; set; }

    public double Lat { get; set; }

    public double Lon { get; set; }

    public Guid UserId { get; set; }

    public string Text { get; set; }

    public Guid TileId { get; set; }

    public NoteStatus Status { get; set; }

    public bool Editable { get; set; } = false;
}