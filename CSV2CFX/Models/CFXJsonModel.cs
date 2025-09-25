using System.Text.Json;

namespace CSV2CFX.Models
{
    public class CFXJsonModel
    {
        public string? MessageName { get; set; }

        public string? Version { get; set; }

        public string? TimeStamp { get; set; } = DateTime.UtcNow.ToString();

        public string? UniqueID { get; set; }

        public string? Source { get; set; }

        public string? Target { get; set; }

        public string? RequestID { get; set; }

        public Dictionary<string, dynamic?>? MessageBody { get; set; }
    }
}
