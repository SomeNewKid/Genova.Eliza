// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents an alternation token that matches any one of several literal words.
/// </summary>
internal sealed record AltsToken : PatternToken
{
    /// <summary>
    /// Gets or sets the list of alternative literal words.
    /// </summary>
    [JsonPropertyName("alts")]
    public List<string> Alts { get; set; } = [];
}
