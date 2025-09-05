// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza;

/// <summary>
/// Represents a keyword entry in the ELIZA script, including its optional
/// substitution, tags, precedence, and associated decomposition rules.
/// </summary>
/// <remarks>
/// A keyword may specify:
/// <list type="bullet">
///   <item>
///     <description>
///       A <see cref="Substitution"/> word applied at keyword match time
///       (for example, <c>(DREAMS = DREAM)</c>).
///     </description>
///   </item>
///   <item>
///     <description>
///       Optional <see cref="DList"/> tags (e.g., part-of-speech or families)
///       used by pattern tag tokens.
///     </description>
///   </item>
///   <item>
///     <description>
///       An optional <see cref="Precedence"/> number to influence selection when
///       multiple keywords match, where higher numbers take priority.
///     </description>
///   </item>
///   <item>
///     <description>
///       One or more <see cref="Decompositions"/> that define how user input
///       is matched and reassembled (or linked to another keyword).
///     </description>
///   </item>
/// </list>
/// </remarks>
internal sealed class KeywordEntry
{
    /// <summary>
    /// Gets the canonical keyword token.
    /// </summary>
    [JsonPropertyName("keyword")]
    public string Keyword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the synonym substitution word applied at keyword level (for example, <c>(DREAMS = DREAM)</c>).
    /// </summary>
    [JsonPropertyName("substitution")]
    public string? Substitution { get; init; }

    /// <summary>
    /// Gets the optional tag list (DLIST), such as <c>["NOUN","FAMILY"]</c>, associated with this keyword.
    /// </summary>
    [JsonPropertyName("dlist")]
    public List<string>? DList { get; init; }

    /// <summary>
    /// Gets the optional precedence value used to prefer this keyword over others when multiple matches occur.
    /// </summary>
    [JsonPropertyName("precedence")]
    public int? Precedence { get; init; }

    /// <summary>
    /// Gets the ordered collection of decomposition rules associated with this keyword.
    /// </summary>
    /// <remarks>
    /// The order of decompositions matters; the engine evaluates them in sequence.
    /// For pattern-based decompositions, reassembly selection is typically round-robin per decomposition.
    /// </remarks>
    [JsonPropertyName("decompositions")]
    public List<Decomposition> Decompositions { get; init; } = [];
}
