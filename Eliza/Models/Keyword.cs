// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a keyword rule in the ELIZA “DOCTOR” script.
/// A keyword may define a substitution, a precedence rank, one or more
/// decomposition/reassembly rules, or act as a link to another keyword.
/// </summary>
internal sealed class Keyword
{
    /// <summary>
    /// Gets or sets the surface keyword that triggers this rule.
    /// Example: <c>"COMPUTER"</c> or <c>"MY"</c>.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the canonical substitution for this keyword, if any.
    /// Example: <c>"YOU'RE"</c> may substitute to <c>"I'M"</c>.
    /// </summary>
    [JsonPropertyName("substitution")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Substitution { get; set; }

    /// <summary>
    /// Gets or sets the precedence rank for this keyword.
    /// Higher values indicate higher priority when multiple keywords match.
    /// </summary>
    [JsonPropertyName("rank")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rank { get; set; }

    /// <summary>
    /// Gets or sets the list of decomposition patterns and their associated
    /// reassemblies for this keyword. If null, this keyword may only act as a link.
    /// </summary>
    [JsonPropertyName("decompositions")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<Decomposition>? Decompositions { get; set; }

    /// <summary>
    /// Gets or sets the equivalence link target, if this keyword
    /// redirects to another keyword’s rule set.
    /// </summary>
    [JsonPropertyName("link")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Link { get; set; }
}
