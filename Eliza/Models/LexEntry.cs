// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a single lexicon entry in the ELIZA “DOCTOR” script.
/// A lexicon entry may specify a substitution for the word and/or
/// one or more semantic or grammatical tags used in decomposition patterns.
/// </summary>
internal sealed class LexEntry
{
    /// <summary>
    /// Gets or sets the canonical substitution for this word, if any.
    /// For example, <c>"mom"</c> may have the substitution <c>"mother"</c>.
    /// If null, the word is treated as-is.
    /// </summary>
    [JsonPropertyName("substitution")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Substitution { get; set; }

    /// <summary>
    /// Gets or sets the list of tags associated with this word.
    /// Tags allow decomposition rules to match categories of words
    /// (for example, <c>/FAMILY</c> or <c>/BELIEF</c>).
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];
}
