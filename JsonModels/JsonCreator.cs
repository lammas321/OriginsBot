using System.Text.Json;
using System.Text.Json.Serialization;

namespace OriginsBot.JsonModels
{
    [JsonConverter(typeof(Converter))]
    public sealed class JsonCreator
    {
        public required string Name { get; set; }

        public required string Description { get; set; }

        public required JsonInfo[] Info { get; set; }
        
        public required uint[] Color { get; set; }


        private sealed class Converter : JsonConverter<JsonCreator>
        {
            public override JsonCreator Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement root = doc.RootElement;

                string name = ReadString(root, "name", options);
                string description = ReadString(root, "description", options);
                JsonInfo[] info = ReadInfo(root, options);
                uint[] color = ReadColor(root, options);

                return new()
                {
                    Name = name,
                    Description = description,
                    Info = info,
                    Color = color
                };
            }

            private static string ReadString(JsonElement root, string propName, JsonSerializerOptions options)
            {
                if (!root.TryGetProperty(propName, out JsonElement prop))
                    throw new JsonException($"Json property '{propName}' is missing.");

                if (prop.ValueKind == JsonValueKind.String)
                    return JsonSerializer.Deserialize<string>(prop.GetRawText(), options)
                        ?? throw new JsonException($"Json property '{propName}' failed to deserialize.");

                throw new JsonException($"Json property '{propName}' had the unexpected type of '{prop.ValueKind}'.");
            }

            private static JsonInfo[] ReadInfo(JsonElement root, JsonSerializerOptions options)
            {
                string propName = "info";
                if (
                    !root.TryGetProperty(propName, out JsonElement prop) &&
                    !root.TryGetProperty("meta", out prop))
                    throw new JsonException($"Json property '{propName}' is missing.");

                if (prop.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<JsonInfo[]>(prop.GetRawText(), options)
                        ?? throw new JsonException($"Json property '{propName}' failed to deserialize.");

                if (prop.ValueKind == JsonValueKind.Object)
                    return [.. (JsonSerializer.Deserialize<Dictionary<string, string>>(prop.GetRawText(), options)
                        ?? throw new JsonException($"Json property '{propName}' failed to deserialize."))
                        .Select(kv => new JsonInfo { Name = kv.Key, Value = kv.Value })];

                throw new JsonException($"Json property '{propName}' had the unexpected type of '{prop.ValueKind}'.");
            }
            
            private static uint[] ReadColor(JsonElement root, JsonSerializerOptions options)
            {
                string propName = "color";
                if (!root.TryGetProperty(propName, out JsonElement prop))
                    throw new JsonException($"Json property '{propName}' is missing.");

                if (prop.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<uint[]>(prop.GetRawText(), options)
                        ?? throw new JsonException($"Json property '{propName}' failed to deserialize.");

                throw new JsonException($"Json property '{propName}' had the unexpected type of '{prop.ValueKind}'.");
            }


            public override void Write(Utf8JsonWriter writer, JsonCreator value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("name");
                JsonSerializer.Serialize(writer, value.Name, options);

                writer.WritePropertyName("description");
                JsonSerializer.Serialize(writer, value.Description, options);

                writer.WritePropertyName("info");
                JsonSerializer.Serialize(writer, value.Info, options);

                writer.WritePropertyName("color");
                JsonSerializer.Serialize(writer, value.Color, options);

                writer.WriteEndObject();
            }
        }
    }
}