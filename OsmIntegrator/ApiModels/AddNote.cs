using System;
using System.ComponentModel.DataAnnotations;

public class NewNote
{
    public Guid UserId { get; set; }

    [Required]
    public double? Lat { get; set; }

    [Required]
    public double? Lon { get; set; }

    [Required]
    public Guid? TileId { get; set; }

    public string Text { get; set; }

}