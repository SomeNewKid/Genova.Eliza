// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza;

/// <summary>
/// Represents the MEMORY block in the ELIZA script, including the memory keyword
/// and its associated rules used to record and later recall user statements.
/// </summary>
/// <remarks>
/// The original DOCTOR script defines a single MEMORY section with exactly four rules.
/// This model does not enforce the rule count at the type level; validation should be
/// performed elsewhere if needed.
/// </remarks>
internal sealed class MemoryBlock
{
    /// <summary>
    /// Gets the memory keyword (for example, <c>MY</c>) that triggers creation of a memory.
    /// </summary>
    [JsonPropertyName("keyword")]
    public string Keyword { get; init; } = string.Empty;

    /// <summary>
    /// Gets the ordered list of memory rules associated with the <see cref="Keyword"/>.
    /// </summary>
    [JsonPropertyName("rules")]
    public List<MemoryRule> Rules { get; init; } = [];
}
