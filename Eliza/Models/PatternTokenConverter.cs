// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Provides custom JSON serialization and deserialization for <see cref="PatternToken"/> objects.
/// <para>
/// This converter allows pattern tokens to be represented either as plain strings
/// (for literal tokens) or as objects with specific fields for wildcards, alternations,
/// or tags.
/// </para>
/// </summary>
internal sealed class PatternTokenConverter : JsonConverter<PatternToken>
{
    /// <summary>
    /// Reads and converts JSON to a <see cref="PatternToken"/> instance.
    /// Recognizes the following shapes:
    /// <list type="bullet">
    /// <item><description>String → <see cref="LiteralToken"/></description></item>
    /// <item><description><c>{"wildcard": true}</c> → <see cref="WildcardToken"/></description></item>
    /// <item><description><c>{"alts": [...]}</c> → <see cref="AltsToken"/></description></item>
    /// <item><description><c>{"tag": "..."}</c> → <see cref="TagToken"/></description></item>
    /// </list>
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type being converted (always <see cref="PatternToken"/>).</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>A deserialized <see cref="PatternToken"/> object.</returns>
    /// <exception cref="JsonException">Thrown if the JSON does not represent a valid pattern token.</exception>
    public override PatternToken? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Literal string token
        if (reader.TokenType == JsonTokenType.String)
        {
            string? text = reader.GetString();
            return new LiteralToken { Value = text! };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected string or object for pattern token.");
        }

        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        if (root.TryGetProperty("wildcard", out JsonElement wc) && wc.ValueKind == JsonValueKind.True)
        {
            return new WildcardToken();
        }

        if (root.TryGetProperty("alts", out JsonElement altsElem) && altsElem.ValueKind == JsonValueKind.Array)
        {
            var list = new List<string>();
            foreach (JsonElement a in altsElem.EnumerateArray())
            {
                list.Add(a.GetString() ?? string.Empty);
            }

            return new AltsToken { Alts = list };
        }

        if (root.TryGetProperty("tag", out JsonElement tagElem) && tagElem.ValueKind == JsonValueKind.String)
        {
            return new TagToken { Tag = tagElem.GetString() ?? string.Empty };
        }

        throw new JsonException("Unrecognized pattern token shape.");
    }

    /// <summary>
    /// Writes a <see cref="PatternToken"/> instance to JSON.
    /// Serializes tokens in one of the following forms:
    /// <list type="bullet">
    /// <item><description><see cref="LiteralToken"/> → string</description></item>
    /// <item><description><see cref="WildcardToken"/> → <c>{"wildcard": true}</c></description></item>
    /// <item><description><see cref="AltsToken"/> → <c>{"alts": [...]}</c></description></item>
    /// <item><description><see cref="TagToken"/> → <c>{"tag": "..."}</c></description></item>
    /// </list>
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The <see cref="PatternToken"/> to serialize.</param>
    /// <param name="options">Serializer options.</param>
    /// <exception cref="NotSupportedException">Thrown if the <paramref name="value"/> type is unknown.</exception>
    public override void Write(Utf8JsonWriter writer, PatternToken value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case LiteralToken lit:
                writer.WriteStringValue(lit.Value);
                break;
            case WildcardToken:
                writer.WriteStartObject();
                writer.WriteBoolean("wildcard", true);
                writer.WriteEndObject();
                break;
            case AltsToken alts:
                writer.WriteStartObject();
                writer.WritePropertyName("alts");
                JsonSerializer.Serialize(writer, alts.Alts, options);
                writer.WriteEndObject();
                break;
            case TagToken tag:
                writer.WriteStartObject();
                writer.WriteString("tag", tag.Tag);
                writer.WriteEndObject();
                break;
            default:
                throw new NotSupportedException($"Unknown PatternToken: {value.GetType().Name}");
        }
    }
}
