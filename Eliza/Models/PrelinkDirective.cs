// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a prelink directive, which applies a preliminary template
/// before linking to another keyword. Used in the script for persona flips
/// like "YOU'RE = I'M".
/// </summary>
internal sealed record PrelinkDirective : ReassemblyItem
{
    /// <summary>
    /// Gets or sets the prelink payload, containing the intermediate template
    /// and the target keyword link.
    /// </summary>
    [JsonPropertyName("prelink")]
    public Prelink Prelink { get; set; } = new();
}
