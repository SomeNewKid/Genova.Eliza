using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Genova.Eliza;

/// <summary>
/// Converter that reads a JSON array of heterogeneous pattern tokens
/// and produces a List&lt;PatternToken&gt; using the finite set above.
/// </summary>
internal sealed class PatternTokenListConverter : JsonConverter<List<PatternToken>>
{
    public override List<PatternToken>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
            throw new JsonException("Pattern must be a JSON array.");

        var list = new List<PatternToken>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray) break;

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    list.Add(new StringToken(reader.GetString() ?? string.Empty));
                    break;

                case JsonTokenType.StartObject:
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        var obj = doc.RootElement;
                        if (obj.TryGetProperty("set", out var setProp) && setProp.ValueKind == JsonValueKind.Array)
                        {
                            var items = new List<string>();
                            foreach (var v in setProp.EnumerateArray())
                                items.Add(v.GetString() ?? string.Empty);
                            list.Add(new SetToken(items));
                        }
                        else if (obj.TryGetProperty("tag", out var tagProp) && tagProp.ValueKind == JsonValueKind.String)
                        {
                            list.Add(new TagToken(tagProp.GetString() ?? string.Empty));
                        }
                        else if (obj.TryGetProperty("tags", out var tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                        {
                            var tags = new List<string>();
                            foreach (var v in tagsProp.EnumerateArray())
                                tags.Add(v.GetString() ?? string.Empty);
                            list.Add(new TagToken(tags));
                        }
                        else
                        {
                            throw new JsonException("Unknown pattern token object.");
                        }
                    }
                    break;

                default:
                    throw new JsonException("Unsupported token in pattern array.");
            }
        }

        return list;
    }

    public override void Write(Utf8JsonWriter writer, List<PatternToken> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var tok in value)
        {
            switch (tok)
            {
                case StringToken s:
                    writer.WriteStringValue(s.Text);
                    break;
                case SetToken set:
                    writer.WriteStartObject();
                    writer.WritePropertyName("set");
                    writer.WriteStartArray();
                    foreach (var it in set.Items) writer.WriteStringValue(it);
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                    break;
                case TagToken tag:
                    writer.WriteStartObject();
                    if (tag.Tags.Count == 1)
                    {
                        writer.WriteString("tag", tag.Tags[0]);
                    }
                    else
                    {
                        writer.WritePropertyName("tags");
                        writer.WriteStartArray();
                        foreach (var t in tag.Tags) writer.WriteStringValue(t);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndObject();
                    break;
                default:
                    throw new NotSupportedException($"Unknown PatternToken: {tok?.GetType().Name}");
            }
        }
        writer.WriteEndArray();
    }
}
