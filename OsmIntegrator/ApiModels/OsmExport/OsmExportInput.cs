using System.ComponentModel.DataAnnotations;

public class OsmExportInput
{
  [Key]
  [Required]
  [EmailAddress]
  public string Email { get; set; }

  [Required]
  public string Password { get; set; }

  [Required]
  public string Comment { get; set; }
}