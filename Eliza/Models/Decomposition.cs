// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a decomposition rule within a keyword in the ELIZA “DOCTOR” script.
/// A decomposition consists of a pattern to match against the user’s input
/// and a list of reassembly rules that generate responses when the pattern matches.
/// </summary>
internal sealed class Decomposition
{
    /// <summary>
    /// Gets or sets the pattern tokens that define how this decomposition
    /// matches parts of the user’s input. Tokens may be literals, wildcards,
    /// alternations, or tagged categories.
    /// </summary>
    [JsonPropertyName("pattern")]
    public List<PatternToken> Pattern { get; set; } = [];

    /// <summary>
    /// Gets or sets the list of reassembly items associated with this decomposition.
    /// When the pattern matches, one of these items is chosen to generate a reply.
    /// Items may be plain text templates or directive objects (e.g., link, newkey).
    /// </summary>
    [JsonPropertyName("reassemblies")]
    public List<ReassemblyItem> Reassemblies { get; set; } = [];
}
