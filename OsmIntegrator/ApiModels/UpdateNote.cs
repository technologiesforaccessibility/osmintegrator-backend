using System;
using System.ComponentModel.DataAnnotations;

public class UpdateNote
{
    [Required]
    public Guid Id { get; set; }

    [Required]
    public double Lat { get; set; }

    [Required]
    public double Lon { get; set; }

    [Required]
    public string Text { get; set; }
}