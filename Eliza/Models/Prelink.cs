// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents the payload of a prelink directive.
/// Contains the intermediate template and the link target.
/// </summary>
internal sealed record Prelink
{
    /// <summary>
    /// Gets or sets the intermediate template applied before linking.
    /// </summary>
    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target keyword to link to after the template is applied.
    /// </summary>
    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;
}
