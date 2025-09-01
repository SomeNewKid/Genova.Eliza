// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents the NEWKEY directive.
/// Instructs the engine to abandon the current keyword and continue
/// with the next best matching keyword.
/// </summary>
internal sealed record NewKeyDirective : ReassemblyItem
{
    /// <summary>
    /// Gets or sets a value indicating whether this is a NEWKEY directive.
    /// Always true for this type.
    /// </summary>
    [JsonPropertyName("newkey")]
    public bool NewKey { get; set; } = true;
}
