using System.Collections.Generic;

public class OsmChangeOutput
{
  public string Changes { get; set; }
  public IReadOnlyDictionary<string, string> Tags { get; set; }
}