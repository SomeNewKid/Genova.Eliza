// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a memory (R6) rule in the ELIZA “DOCTOR” script.
/// When the <see cref="Pattern"/> matches the user input, the <see cref="Template"/>
/// is queued for later use by the conversation engine.
/// </summary>
internal sealed class MemoryRule
{
    /// <summary>
    /// Gets or sets the keyword label associated with this memory rule.
    /// This typically mirrors the script’s memory section (e.g., <c>"MY"</c>).
    /// </summary>
    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the decomposition pattern tokens used to match user input.
    /// Tokens can be literals, wildcards, alternations, or tagged categories.
    /// </summary>
    [JsonPropertyName("pattern")]
    public List<PatternToken> Pattern { get; set; } = [];

    /// <summary>
    /// Gets or sets the template text to store when the pattern matches.
    /// May contain capture placeholders (e.g., <c>"$3"</c>) that are resolved
    /// against the matched decomposition segments.
    /// </summary>
    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;
}
