// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza.Enums;

/// <summary>
/// Describes the action to take after processing a reassembly item.
/// </summary>
internal enum DirectiveAction
{
    /// <summary>
    /// No action could be produced.
    /// </summary>
    None = 0,

    /// <summary>
    /// Emit the provided text as the response.
    /// </summary>
    EmitText = 1,

    /// <summary>
    /// Abandon the current keyword and continue with the next best match.
    /// </summary>
    NewKey = 2,

    /// <summary>
    /// Jump to the specified keyword/equivalence target.
    /// </summary>
    Link = 3,

    /// <summary>
    /// Apply an intermediate template to transform input, then jump to a target keyword.
    /// </summary>
    Prelink = 4,
}
