// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza;

/// <summary>
/// Represents a literal string token used in ELIZA decomposition patterns.
/// </summary>
/// <remarks>
/// Typical examples include tokens such as <c>"YOUR"</c> or <c>"ARE"</c>.
/// The engine may also treat the string <c>"0"</c> specially as a wildcard
/// placeholder when matching patterns.
/// </remarks>
internal sealed record StringToken : PatternToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringToken"/> class with the specified literal text.
    /// </summary>
    /// <param name="text">The literal text of the token.</param>
    public StringToken(string text) => Text = text;

    /// <summary>
    /// Gets the literal text of the token.
    /// </summary>
    public string Text { get; init; }
}
