// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza;

/// <summary>
/// Represents a set-membership pattern token used in ELIZA decomposition patterns.
/// </summary>
/// <remarks>
/// This token matches exactly one input token if (and only if) that token is contained in
/// the provided set. It corresponds to the JSON shape <c>{ "set": ["MOTHER","FATHER", ...] }</c>.
/// </remarks>
internal sealed record SetToken : PatternToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetToken"/> class with the specified collection of
    /// candidate tokens.
    /// </summary>
    /// <param name="items">
    /// The collection of candidate tokens that constitute the set to test membership against.
    /// </param>
    public SetToken(List<string> items) => Items = items;

    /// <summary>
    /// Gets the collection of candidate tokens that this pattern token uses for single-token membership matching.
    /// </summary>
    public List<string> Items { get; init; }
}
