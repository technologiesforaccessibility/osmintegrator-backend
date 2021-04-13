using System.ComponentModel.DataAnnotations;
using OsmIntegrator.Enums;

namespace OsmIntegrator.ApiModels
{
    public class ConnectionAction : Connection
    {
        [Required]
        public ConnectionType? ConnectionType { get; set; }
    }
}