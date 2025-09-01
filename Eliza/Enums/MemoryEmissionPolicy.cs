// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza.Enums;

/// <summary>
/// Defines when queued memory responses should be emitted.
/// </summary>
internal enum MemoryEmissionPolicy
{
    /// <summary>
    /// Memory handling is disabled; never emit queued responses.
    /// </summary>
    Off = 0,

    /// <summary>
    /// Emit a memory response only when no other keyword produced a reply (fallback case).
    /// </summary>
    FallbackOnly = 1,

    /// <summary>
    /// Attempt to interleave a memory response every N turns (see <see cref="MemoryManager.InterleaveEvery"/>).
    /// </summary>
    InterleaveEveryN = 2,

    /// <summary>
    /// Emit whenever a memory response is available (caller still controls when to call <c>TryDequeue</c>).
    /// </summary>
    Opportunistic = 3,
}
