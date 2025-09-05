// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza;

/// <summary>
/// Represents a tag-based pattern token used in ELIZA decomposition patterns.
/// </summary>
/// <remarks>
/// This token matches exactly one input token if that token belongs to one or more
/// predefined tag groups (derived from a DLIST). It corresponds to the JSON shapes
/// <c>{ "tag": "BELIEF" }</c> or <c>{ "tags": ["NOUN","FAMILY"] }</c>.
/// </remarks>
internal sealed record TagToken : PatternToken
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TagToken"/> class with the specified collection of tag names.
    /// </summary>
    /// <param name="tags">The tag names used to test single-token membership.</param>
    public TagToken(List<string> tags) => Tags = tags;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagToken"/> class with a single tag name.
    /// </summary>
    /// <param name="singleTag">The single tag name used to test single-token membership.</param>
    public TagToken(string singleTag)
        : this([singleTag])
    {
    }

    /// <summary>
    /// Gets the collection of tag names used to test single-token membership.
    /// </summary>
    public List<string> Tags { get; init; } = [];
}
