// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Base type for pattern tokens used in decomposition rules of the ELIZA “DOCTOR” script.
/// A pattern token defines one element of the matching expression, which may be a literal,
/// a wildcard, an alternation, or a tag reference.
/// </summary>
[JsonConverter(typeof(PatternTokenConverter))]
internal abstract record PatternToken;
