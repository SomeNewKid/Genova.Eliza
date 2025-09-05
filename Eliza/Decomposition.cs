// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza;

/// <summary>
/// Represents a single decomposition rule in the ELIZA script.
/// </summary>
/// <remarks>
/// A decomposition can be:
/// <list type="bullet">
///   <item>
///     <description>
///       A link-only rule (see <see cref="Link"/>), which transfers control to another keyword.
///     </description>
///   </item>
///   <item>
///     <description>
///       A pattern-based rule (see <see cref="Pattern"/>) with one or more reassembly templates
///       (see <see cref="Reassembly"/>).
///     </description>
///   </item>
///   <item>
///     <description>
///       A “pre + link” rule (see <see cref="Pre"/> and <see cref="Link"/>), which assembles a preface
///       and then follows a link.
///     </description>
///   </item>
/// </list>
/// </remarks>
internal sealed class Decomposition
{
    /// <summary>
    /// Gets the target keyword to link to when this decomposition represents a link (i.e., <c>=WORD</c>).
    /// </summary>
    [JsonPropertyName("link")]
    public string? Link { get; init; }

    /// <summary>
    /// Gets the ordered pattern tokens to match against the user's input.
    /// </summary>
    /// <remarks>
    /// Tokens may include string literals and structured tokens represented by <see cref="PatternToken"/>,
    /// such as sets and tags.
    /// </remarks>
    [JsonPropertyName("pattern")]
    public List<PatternToken>? Pattern { get; init; }

    /// <summary>
    /// Gets the ordered list of reassembly templates used when the pattern matches.
    /// </summary>
    [JsonPropertyName("reassembly")]
    public List<string>? Reassembly { get; init; }

    /// <summary>
    /// Gets the preface text to assemble prior to following the <see cref="Link"/>.
    /// </summary>
    /// <remarks>
    /// Used by “PRE + link” rules. When present, the engine assembles this text with captured segments and
    /// retokenizes it as the new input before evaluating the linked keyword.
    /// </remarks>
    [JsonPropertyName("pre")]
    public string? Pre { get; init; }
}
