// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a plain text reassembly template.
/// May include capture placeholders (e.g., "$1", "$2") referring
/// to segments of the matched decomposition pattern.
/// </summary>
internal sealed record TemplateItem : ReassemblyItem
{
    /// <summary>
    /// Gets or sets the template text used to build the reply.
    /// </summary>
    [JsonPropertyName("template")]
    public string Template { get; set; } = string.Empty;
}
