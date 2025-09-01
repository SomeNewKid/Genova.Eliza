// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents the three substitution tables used by the ELIZA “DOCTOR” script.
/// <list type="bullet">
/// <item>
/// <description><c>Simple</c>: lexical normalization of contractions and synonyms (e.g., "dont" → "don't").
/// </description>
/// </item>
/// <item>
/// <description><c>Pre</c>: substitutions applied to user input before keyword matching, typically pronoun/persona
/// flips (e.g., "I" → "you").</description>
/// </item>
/// <item>
/// <description><c>Post</c>: substitutions applied to generated output just before presentation, usually reversing
/// pronoun flips (e.g., "you" → "I").</description>
/// </item>
/// </list>
/// </summary>
internal sealed class Substitutions
{
    /// <summary>
    /// Gets or sets simple normalization substitutions.
    /// Maps surface forms to canonical equivalents without changing viewpoint.
    /// Example: <c>{ "cant": "can't", "recollect": "remember" }</c>.
    /// </summary>
    [JsonPropertyName("simple")]
    public Dictionary<string, string> Simple { get; set; } = [];

    /// <summary>
    /// Gets or sets pre-processing substitutions applied to user input before decomposition.
    /// Typically handles persona and pronoun swaps.
    /// Example: <c>{ "i": "you", "my": "your" }</c>.
    /// </summary>
    [JsonPropertyName("pre")]
    public Dictionary<string, string> Pre { get; set; } = [];

    /// <summary>
    /// Gets or sets post-processing substitutions applied to generated replies before output.
    /// Usually reverses the pre-processing flips to produce natural dialogue.
    /// Example: <c>{ "you": "I", "your": "my" }</c>.
    /// </summary>
    [JsonPropertyName("post")]
    public Dictionary<string, string> Post { get; set; } = [];
}
