// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Represents a literal token that matches an exact word in the user input.
/// </summary>
internal sealed record LiteralToken : PatternToken
{
    /// <summary>
    /// Gets or sets the literal value that must appear in the user input.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
