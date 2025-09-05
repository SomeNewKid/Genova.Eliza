// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza;

/// <summary>
/// Represents a MEMORY rule consisting of a pattern and a single reassembly template.
/// </summary>
/// <remarks>
/// When the <see cref="Pattern"/> matches the user's input during memory creation,
/// the engine assembles a response using <see cref="Reassembly"/> and stores it for
/// later recall.
/// </remarks>
internal sealed class MemoryRule
{
    /// <summary>
    /// Gets the ordered pattern tokens used to match the user's input when creating a memory.
    /// </summary>
    /// <remarks>
    /// The pattern may contain string literals and structured tokens represented by
    /// <see cref="PatternToken"/>, such as sets and tags.
    /// </remarks>
    [JsonPropertyName("pattern")]
    public List<PatternToken> Pattern { get; init; } = [];

    /// <summary>
    /// Gets the reassembly template applied when the <see cref="Pattern"/> matches.
    /// </summary>
    [JsonPropertyName("reassembly")]
    public string Reassembly { get; init; } = string.Empty;
}
