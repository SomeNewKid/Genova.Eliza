// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza;

/// <summary>
/// Represents the abstract base type for pattern tokens used in ELIZA decomposition patterns.
/// </summary>
/// <remarks>
/// This type participates in polymorphic JSON serialization using the <c>$kind</c> discriminator:
/// <list type="bullet">
///   <item><description><c>"s"</c> → <see cref="StringToken"/> (literal string token)</description></item>
///   <item><description><c>"set"</c> → <see cref="SetToken"/> (single-token membership from a set)</description></item>
///   <item><description><c>"tag"</c> → <see cref="TagToken"/> (single-token membership from one or more tags)</description></item>
/// </list>
/// Engine code matches inputs against sequences of <see cref="PatternToken"/> instances when evaluating
/// decomposition rules.
/// </remarks>
/// <seealso cref="StringToken"/>
/// <seealso cref="SetToken"/>
/// <seealso cref="TagToken"/>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(StringToken), typeDiscriminator: "s")]
[JsonDerivedType(typeof(SetToken), typeDiscriminator: "set")]
[JsonDerivedType(typeof(TagToken), typeDiscriminator: "tag")]
internal abstract record PatternToken;
