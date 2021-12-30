using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace OsmIntegrator.Tools
{

  [XmlRoot(ElementName = "osmChange")]
  public class OsmChange
  {
    [XmlAttribute(AttributeName = "version")]
    public string Version { get; set; }
    [XmlAttribute(AttributeName = "comment")]
    public string Comment { get; set; } = "Bus and tram stops tags update";
    [XmlAttribute(AttributeName = "mechanical")]
    public string Mechanical { get; set; } = "yes";
    [XmlAttribute(AttributeName = "generator")]
    public string Generator { get; set; }
    [XmlElement(ElementName = "modify")]
    public Modify Mod { get; set; }
  }
  [XmlRoot(ElementName = "modify")]
  public class Modify
  {
    [XmlElement(ElementName = "node")]
    public List<Node> Nodes { get; set; }
  }

  [XmlRoot(ElementName = "meta")]
  public class Meta
  {
    [XmlAttribute(AttributeName = "osm_base")]
    public string Osm_base { get; set; }
  }
  [XmlRoot(ElementName = "tag")]
  public class Tag
  {
    [XmlAttribute(AttributeName = "k")]
    public string K { get; set; }
    [XmlAttribute(AttributeName = "v")]
    public string V { get; set; }
  }

  [XmlRoot(ElementName = "node")]
  public class Node
  {
    [XmlElement(ElementName = "tag")]
    public List<Tag> Tag { get; set; }
    [XmlAttribute(AttributeName = "id")]
    public string Id { get; set; }
    [XmlAttribute(AttributeName = "lat")]
    public string Lat { get; set; }
    [XmlAttribute(AttributeName = "lon")]
    public string Lon { get; set; }
    [XmlAttribute(AttributeName = "version")]
    public int Version { get; set; }
    [XmlAttribute(AttributeName = "changeset")]
    public string Changeset { get; set; }
  }

  [XmlRoot(ElementName = "osm")]
  public class Osm
  {
    [XmlElement(ElementName = "note")]
    public string Note { get; set; }
    [XmlElement(ElementName = "meta")]
    public Meta Meta { get; set; }
    [XmlElement(ElementName = "node")]
    public List<Node> Node { get; set; }
    [XmlAttribute(AttributeName = "version")]
    public string Version { get; set; }
    [XmlAttribute(AttributeName = "generator")]
    public string Generator { get; set; }
  }

}
