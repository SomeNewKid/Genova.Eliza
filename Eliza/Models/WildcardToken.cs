// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a wildcard token that matches zero or more words in the user input.
/// </summary>
internal sealed record WildcardToken : PatternToken
{
    /// <summary>
    /// Gets or sets a value indicating whether this token is a wildcard.
    /// Always true for wildcard tokens.
    /// </summary>
    [JsonPropertyName("wildcard")]
    public bool Wildcard { get; set; } = true;
}
