// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a link directive, which jumps to another keyword’s reassembly set.
/// Example: <c>{"link": "WHAT"}</c>.
/// </summary>
internal sealed record LinkDirective : ReassemblyItem
{
    /// <summary>
    /// Gets or sets the target keyword name to link to.
    /// </summary>
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;
}
