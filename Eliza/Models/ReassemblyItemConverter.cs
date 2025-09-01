// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Provides custom JSON serialization and deserialization for <see cref="ReassemblyItem"/> objects.
/// <para>
/// This converter supports two JSON shapes:
/// </para>
/// <list type="bullet">
/// <item><description>Plain string → <see cref="TemplateItem"/></description></item>
/// <item><description>Object → one of:
/// <br/>• <c>{"newkey": true}</c> → <see cref="NewKeyDirective"/>
/// <br/>• <c>{"link": "TARGET"}</c> → <see cref="LinkDirective"/>
/// <br/>• <c>{"prelink": {"template":"...","link":"TARGET"}}</c> → <see cref="PrelinkDirective"/></description></item>
/// </list>
/// </summary>
internal sealed class ReassemblyItemConverter : JsonConverter<ReassemblyItem>
{
    /// <summary>
    /// Reads JSON and constructs a <see cref="ReassemblyItem"/> instance.
    /// </summary>
    /// <param name="reader">The JSON reader.</param>
    /// <param name="typeToConvert">The type to convert (always <see cref="ReassemblyItem"/>).</param>
    /// <param name="options">Serializer options.</param>
    /// <returns>A deserialized <see cref="ReassemblyItem"/>.</returns>
    /// <exception cref="JsonException">Thrown when the JSON shape is not recognized.</exception>
    public override ReassemblyItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Plain string ⇒ TemplateItem
        if (reader.TokenType == JsonTokenType.String)
        {
            string text = reader.GetString() ?? string.Empty;
            return new TemplateItem { Template = text };
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected string or object for reassembly item.");
        }

        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        // {"newkey": true}
        if (root.TryGetProperty("newkey", out JsonElement nkElem) &&
            nkElem.ValueKind == JsonValueKind.True)
        {
            return new NewKeyDirective();
        }

        // {"link": "TARGET"}
        if (root.TryGetProperty("link", out JsonElement linkElem) &&
            linkElem.ValueKind == JsonValueKind.String)
        {
            return new LinkDirective { Link = linkElem.GetString() ?? string.Empty };
        }

        // {"prelink": { "template": "...", "link": "TARGET" }}
        if (root.TryGetProperty("prelink", out JsonElement preElem) &&
            preElem.ValueKind == JsonValueKind.Object)
        {
            Prelink pre = JsonSerializer.Deserialize<Prelink>(preElem.GetRawText(), options)
                      ?? throw new JsonException("Invalid 'prelink' object.");
            return new PrelinkDirective { Prelink = pre };
        }

        throw new JsonException("Unrecognized reassembly item shape.");
    }

    /// <summary>
    /// Writes a <see cref="ReassemblyItem"/> instance to JSON using the supported shapes.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The reassembly item to serialize.</param>
    /// <param name="options">Serializer options.</param>
    /// <exception cref="NotSupportedException">Thrown when the item type is not supported by this converter.</exception>
    public override void Write(Utf8JsonWriter writer, ReassemblyItem value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case TemplateItem t:
                writer.WriteStringValue(t.Template);
                return;

            case NewKeyDirective:
                writer.WriteStartObject();
                writer.WriteBoolean("newkey", true);
                writer.WriteEndObject();
                return;

            case LinkDirective l:
                writer.WriteStartObject();
                writer.WriteString("link", l.Link);
                writer.WriteEndObject();
                return;

            case PrelinkDirective p:
                writer.WriteStartObject();
                writer.WritePropertyName("prelink");
                JsonSerializer.Serialize(writer, p.Prelink, options);
                writer.WriteEndObject();
                return;

            default:
                throw new NotSupportedException($"Unknown ReassemblyItem type: {value.GetType().Name}");
        }
    }
}
