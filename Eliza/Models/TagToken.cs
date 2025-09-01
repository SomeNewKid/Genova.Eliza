// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a tag token that matches any word belonging to a tagged lexicon category.
/// Example: <c>/FAMILY</c> or <c>/BELIEF</c>.
/// </summary>
internal sealed record TagToken : PatternToken
{
    /// <summary>
    /// Gets or sets the name of the tag to match.
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; } = string.Empty;
}
