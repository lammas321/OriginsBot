using System.Text.Json;
using System.Text.Json.Serialization;

namespace OriginsBot.JsonModels
{
    [JsonConverter(typeof(Converter))]
    public sealed class JsonPack
    {
        public required string Name { get; set; }
        
        public required string Description { get; set; }

        public required ulong[] CreatorIds { get; set; }

        public required string Version { get; set; }

        public required string[] Requirements { get; set; }

        public required JsonInfo[] Info { get; set; }

        public required uint[] Color { get; set; }

        public required JsonOrigin[] Origins { get; set; }


        private sealed class Converter : JsonConverter<JsonPack>
        {
            public override JsonPack Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using JsonDocument doc = JsonDocument.ParseValue(ref reader);
                JsonElement root = doc.RootElement;

                string name = ReadString(root, "name", options);
                string description = ReadString(root, "description", options);
                ulong[] creatorIds = ReadCreatorIds(root, options);
                string version = ReadString(root, "version", options);
                string[] requirements = ReadStringArray(root, "requirements", options);
                JsonInfo[] info = ReadInfo(root, options);
                uint[] color = ReadColor(root, options);
                JsonOrigin[] origins = ReadOrigins(root, options);

                return new()
                {
                    Name = name,
                    Description = description,
                    CreatorIds = creatorIds,
                    Version = version,
                    Requirements = requirements,
                    Info = info,
                    Color = color,
                    Origins = origins
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

            private static string[] ReadStringArray(JsonElement root, string propName, JsonSerializerOptions options)
            {
                if (!root.TryGetProperty(propName, out JsonElement prop))
                    throw new JsonException($"Json property '{propName}' is missing.");

                if (prop.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<string[]>(prop.GetRawText(), options)
                        ?? throw new JsonException($"Json property '{propName}' failed to deserialize.");

                throw new JsonException($"Json property '{propName}' had the unexpected type of '{prop.ValueKind}'.");
            }

            private static ulong[] ReadCreatorIds(JsonElement root, JsonSerializerOptions options)
            {
                string propName = "creator_ids";
                if (!root.TryGetProperty(propName, out JsonElement prop))
                    throw new JsonException($"Json property '{propName}' is missing.");

                if (prop.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<ulong[]>(prop.GetRawText(), options)
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

            private static JsonOrigin[] ReadOrigins(JsonElement root, JsonSerializerOptions options)
            {
                string propName = "origins";
                if (!root.TryGetProperty(propName, out JsonElement prop))
                    throw new JsonException($"Json property '{propName}' is missing.");

                if (prop.ValueKind == JsonValueKind.Array)
                    return JsonSerializer.Deserialize<JsonOrigin[]>(prop.GetRawText(), options)
                        ?? throw new JsonException($"Json property '{propName}' failed to deserialize.");

                if (prop.ValueKind == JsonValueKind.Object)
                {
                    List<JsonOrigin> origins = [];

                    foreach (var kv in prop.EnumerateObject())
                    {
                        string id = kv.Name;
                        byte impact = ReadImpact(kv.Value, options);
                        string[] powers = ReadStringArray(kv.Value, "powers", options);
                        uint[] color = ReadColor(kv.Value, options);

                        origins.Add(new JsonOrigin
                        {
                            Id = id,
                            Impact = impact,
                            PowerIds = powers,
                            Color = color
                        });
                    }

                    return [.. origins];
                }

                throw new JsonException($"Json property '{propName}' had the unexpected type of '{prop.ValueKind}'.");
            }

            private static byte ReadImpact(JsonElement root, JsonSerializerOptions options)
            {
                string propName = "impact";
                if (!root.TryGetProperty(propName, out JsonElement prop))
                    throw new JsonException($"Json property '{propName}' is missing.");

                if (prop.ValueKind == JsonValueKind.Number)
                    return JsonSerializer.Deserialize<byte?>(prop.GetRawText(), options)
                        ?? throw new JsonException($"Json property '{propName}' failed to deserialize.");

                throw new JsonException($"Json property '{propName}' had the unexpected type of '{prop.ValueKind}'.");
            }


            public override void Write(Utf8JsonWriter writer, JsonPack value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("name");
                JsonSerializer.Serialize(writer, value.Name, options);

                writer.WritePropertyName("description");
                JsonSerializer.Serialize(writer, value.Description, options);

                writer.WritePropertyName("creator_ids");
                JsonSerializer.Serialize(writer, value.CreatorIds, options);

                writer.WritePropertyName("version");
                JsonSerializer.Serialize(writer, value.Version, options);

                writer.WritePropertyName("requirements");
                JsonSerializer.Serialize(writer, value.Requirements, options);

                writer.WritePropertyName("info");
                JsonSerializer.Serialize(writer, value.Info, options);

                writer.WritePropertyName("color");
                JsonSerializer.Serialize(writer, value.Color, options);

                writer.WritePropertyName("origins");
                JsonSerializer.Serialize(writer, value.Origins, options);

                writer.WriteEndObject();
            }
        }
    }
}