using System.Text.Json.Serialization;

namespace OriginsBot.JsonModels
{
    public sealed class JsonInfo
    {
        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("value")]
        public required string Value { get; set; }
    }
}