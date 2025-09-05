// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Genova.Eliza;

/// <summary>
/// Converts a JSON array of heterogeneous pattern tokens into a <see cref="List{T}"/> of
/// <see cref="PatternToken"/> instances, and vice versa.
/// </summary>
/// <remarks>
/// The converter supports the following JSON token shapes:
/// <list type="bullet">
///   <item><description><c>"literal"</c> → <see cref="StringToken"/></description></item>
///   <item><description><c>{ "set": ["A","B", ...] }</c> → <see cref="SetToken"/></description></item>
///   <item><description><c>{ "tag": "BELIEF" }</c> or <c>{ "tags": ["NOUN","FAMILY"] }</c> → <see cref="TagToken"/></description></item>
/// </list>
/// During deserialization, the converter reads each array element and instantiates the corresponding
/// <see cref="PatternToken"/>. During serialization, it emits the appropriate JSON shape for each
/// concrete token type.
/// </remarks>
internal sealed class PatternTokenListConverter : JsonConverter<List<PatternToken>>
{
    /// <summary>
    /// Reads a JSON array and produces a list of <see cref="PatternToken"/> instances.
    /// </summary>
    /// <param name="reader">The JSON reader positioned at the start of the array.</param>
    /// <param name="typeToConvert">The target type to convert to (ignored; always a list of <see cref="PatternToken"/>).</param>
    /// <param name="options">Serializer options used for reading.</param>
    /// <returns>
    /// A list of <see cref="PatternToken"/> instances deserialized from the JSON array.
    /// </returns>
    /// <exception cref="JsonException">
    /// Thrown when the JSON is not an array, contains an unsupported token kind, or an object does not
    /// conform to one of the recognized token shapes (<c>set</c>, <c>tag</c>, or <c>tags</c>).
    /// </exception>
    public override List<PatternToken>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Pattern must be a JSON array.");
        }

        List<PatternToken> list = [];

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                break;
            }

            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    list.Add(new StringToken(reader.GetString() ?? string.Empty));
                    break;
                }

                case JsonTokenType.StartObject:
                {
                    using (var doc = JsonDocument.ParseValue(ref reader))
                    {
                        JsonElement obj = doc.RootElement;
                        if (obj.TryGetProperty("set", out JsonElement setProp) && setProp.ValueKind == JsonValueKind.Array)
                        {
                            List<string> items = [];
                            foreach (JsonElement v in setProp.EnumerateArray())
                            {
                                items.Add(v.GetString() ?? string.Empty);
                            }

                            list.Add(new SetToken(items));
                        }
                        else if (obj.TryGetProperty("tag", out JsonElement tagProp) && tagProp.ValueKind == JsonValueKind.String)
                        {
                            list.Add(new TagToken(tagProp.GetString() ?? string.Empty));
                        }
                        else if (obj.TryGetProperty("tags", out JsonElement tagsProp) && tagsProp.ValueKind == JsonValueKind.Array)
                        {
                            var tags = new List<string>();
                            foreach (JsonElement v in tagsProp.EnumerateArray())
                            {
                                tags.Add(v.GetString() ?? string.Empty);
                            }

                            list.Add(new TagToken(tags));
                        }
                        else
                        {
                            throw new JsonException("Unknown pattern token object.");
                        }
                    }

                    break;
                }

                default:
                {
                    throw new JsonException("Unsupported token in pattern array.");
                }
            }
        }

        return list;
    }

    /// <summary>
    /// Writes a list of <see cref="PatternToken"/> instances as a JSON array.
    /// </summary>
    /// <param name="writer">The JSON writer to which the array will be written.</param>
    /// <param name="value">The list of <see cref="PatternToken"/> instances to serialize.</param>
    /// <param name="options">Serializer options used for writing.</param>
    /// <exception cref="NotSupportedException">
    /// Thrown when the list contains an unknown <see cref="PatternToken"/> subtype.
    /// </exception>
    public override void Write(Utf8JsonWriter writer, List<PatternToken> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (PatternToken tok in value)
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
                    foreach (string it in set.Items)
                    {
                        writer.WriteStringValue(it);
                    }

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
                        foreach (string t in tag.Tags)
                        {
                            writer.WriteStringValue(t);
                        }

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
