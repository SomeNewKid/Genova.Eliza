// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Base type for reassembly items in the ELIZA “DOCTOR” script.
/// A reassembly item determines how a matched decomposition is turned
/// into a response. Items may be plain text templates, or special directives
/// that control conversation flow (e.g., NEWKEY, link, prelink).
/// </summary>
[JsonConverter(typeof(ReassemblyItemConverter))]
internal abstract record ReassemblyItem;
