using System.Text.Json.Serialization;

namespace OriginsBot.JsonModels
{
    public sealed class JsonOrigin
    {
        [JsonPropertyName("id")]
        public required string Id { get; set; }

        [JsonPropertyName("impact")]
        public required byte Impact { get; set; }

        [JsonPropertyName("power_ids")]
        public required string[] PowerIds { get; set; }
        
        [JsonPropertyName("color")]
        public required uint[] Color { get; set; }
    }
}