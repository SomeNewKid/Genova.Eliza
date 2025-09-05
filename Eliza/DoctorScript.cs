// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Utilities;

namespace Genova.Eliza;

/// <summary>
/// Represents the ELIZA/DOCTOR script loaded from JSON, including the greeting,
/// NONE fallbacks, MEMORY rules, and the set of keyword entries.
/// </summary>
/// <remarks>
/// This type also provides helpers to load a script from an embedded JSON resource
/// and to validate structural invariants expected by the original ELIZA logic.
/// </remarks>
internal sealed class DoctorScript
{
    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> used when deserializing the script,
    /// including converters for polymorphic pattern tokens.
    /// </summary>
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules.")]
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new PatternTokenListConverter() },
    };

    /// <summary>
    /// Gets the greeting line printed by ELIZA at the start of a session.
    /// </summary>
    [JsonPropertyName("hello")]
    public string Hello { get; init; } = string.Empty;

    /// <summary>
    /// Gets the collection of fallback responses used when no keyword matches and no memory is recalled.
    /// </summary>
    [JsonPropertyName("none")]
    public List<string> None { get; init; } = [];

    /// <summary>
    /// Gets the MEMORY block, which contains the memory keyword and its four rules.
    /// </summary>
    [JsonPropertyName("memory")]
    public MemoryBlock Memory { get; init; } = new();

    /// <summary>
    /// Gets the ordered list of keyword entries, each containing decomposition rules and optional metadata.
    /// </summary>
    [JsonPropertyName("keywords")]
    public List<KeywordEntry> Keywords { get; init; } = [];

    /// <summary>
    /// Loads a <see cref="DoctorScript"/> from the `DOCTOR.json` resource located under <c>Data/</c>.
    /// </summary>
    /// <returns>The deserialized <see cref="DoctorScript"/> instance.</returns>
    public static DoctorScript Load()
    {
        return Load("DOCTOR.json");
    }

    /// <summary>
    /// Loads a <see cref="DoctorScript"/> from an embedded JSON resource located under <c>Data/</c>.
    /// </summary>
    /// <param name="filename">The file name of the JSON resource (e.g., <c>DOCTOR.json</c>).</param>
    /// <returns>The deserialized <see cref="DoctorScript"/> instance.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the embedded JSON resource cannot be found.</exception>
    /// <exception cref="InvalidDataException">Thrown when the JSON cannot be deserialized into a valid script.</exception>
    public static DoctorScript Load(string filename)
    {
        string normalizedPath = $"Data/{filename}";

        string? content = null;
        Stream? stream = FileHelper.GetEmbeddedResourceStream(typeof(DoctorScript), normalizedPath);
        if (stream != null)
        {
            using (StreamReader reader = new(stream))
            {
                content = reader.ReadToEnd();
            }
        }

        if (string.IsNullOrEmpty(content))
        {
            throw new FileNotFoundException("JSON file not found", filename);
        }

        DoctorScript? data = JsonSerializer.Deserialize<DoctorScript>(content, Options);
        if (data is null)
        {
            throw new InvalidDataException("Failed to deserialize DOCTOR.json.");
        }

        return data;
    }
}
