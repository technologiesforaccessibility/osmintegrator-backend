using System;

namespace OsmIntegrator.ApiModels
{
    public class Tag
    {
        public Guid Id { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public Guid OsmStopId { get; set; }
        public Stop OsmStop { get; set; }
    }
}