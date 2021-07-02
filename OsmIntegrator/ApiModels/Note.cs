using System;
using System.ComponentModel.DataAnnotations;

public class Note
{
    [Required]
    public double Lat { get; set; }

    [Required]
    public double Lon { get; set; }

    public Guid UserId { get; set; }

    [Required]
    public string Text { get; set; }
}