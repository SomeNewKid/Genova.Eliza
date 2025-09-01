// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Selects reassembly items for matched decompositions, cycling deterministically across
/// successive matches of the same <c>(keyword, decompositionIndex)</c> pair.
/// <para>
/// Persist an instance of this selector per conversation session so that cycling state
/// is preserved across turns.
/// </para>
/// </summary>
internal sealed class ReassemblySelector
{
    private readonly Dictionary<(string key, int decompIndex), int> _cursors;
    private readonly IEqualityComparer<string> _keyComparer;
    private readonly object _gate = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReassemblySelector"/> class.
    /// </summary>
    /// <param name="caseInsensitiveKeys">
    /// If <c>true</c>, keyword keys are compared case-insensitively when tracking cursors.
    /// </param>
    public ReassemblySelector(bool caseInsensitiveKeys = true)
    {
        _keyComparer = caseInsensitiveKeys ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        _cursors = new Dictionary<(string key, int decompIndex), int>(new KeyTupleComparer(_keyComparer));
    }

    /// <summary>
    /// Selects the next reassembly item for the specified keyword and decomposition index,
    /// advancing the internal cursor in a cyclic fashion.
    /// </summary>
    /// <param name="keywordKey">The matched keyword key (e.g., <c>"WHAT"</c>).</param>
    /// <param name="decompositionIndex">The zero-based index of the matched decomposition within the keyword.</param>
    /// <param name="reassemblies">The list of candidate reassembly items for the decomposition.</param>
    /// <returns>The selected <see cref="ReassemblyItem"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="keywordKey"/> or <paramref name="reassemblies"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="reassemblies"/> is empty.</exception>
    public ReassemblyItem Select(string keywordKey, int decompositionIndex, IList<ReassemblyItem> reassemblies)
    {
        ArgumentNullException.ThrowIfNull(keywordKey);
        ArgumentNullException.ThrowIfNull(reassemblies);
        if (reassemblies.Count == 0)
        {
            throw new ArgumentException("Reassemblies must not be empty.", nameof(reassemblies));
        }

        if (decompositionIndex < 0)
        {
            decompositionIndex = 0;
        }

        int idx = GetAndAdvanceIndex(keywordKey, decompositionIndex, reassemblies.Count);
        return reassemblies[idx];
    }

    /// <summary>
    /// Attempts to select the next reassembly item for the specified keyword and decomposition index,
    /// advancing the internal cursor in a cyclic fashion.
    /// </summary>
    /// <param name="keywordKey">The matched keyword key (e.g., <c>"WHAT"</c>).</param>
    /// <param name="decompositionIndex">The zero-based index of the matched decomposition within the keyword.</param>
    /// <param name="reassemblies">The list of candidate reassembly items for the decomposition.</param>
    /// <param name="item">When this method returns, contains the selected item if successful; otherwise <c>null</c>.</param>
    /// <returns><c>true</c> if an item was selected; otherwise <c>false</c>.</returns>
    public bool TrySelect(string keywordKey, int decompositionIndex, IList<ReassemblyItem> reassemblies, out ReassemblyItem? item)
    {
        item = null;
        if (keywordKey is null || reassemblies is null || reassemblies.Count == 0)
        {
            return false;
        }

        int idx = GetAndAdvanceIndex(keywordKey, decompositionIndex < 0 ? 0 : decompositionIndex, reassemblies.Count);
        item = reassemblies[idx];
        return true;
    }

    /// <summary>
    /// Resets the cycling cursor for a specific <c>(keyword, decompositionIndex)</c> pair.
    /// </summary>
    /// <param name="keywordKey">The keyword key.</param>
    /// <param name="decompositionIndex">The zero-based decomposition index within the keyword.</param>
    public void Reset(string keywordKey, int decompositionIndex)
    {
        if (keywordKey is null)
        {
            return;
        }

        (string keywordKey, int) k = (keywordKey, decompositionIndex < 0 ? 0 : decompositionIndex);
        lock (_gate)
        {
            _cursors.Remove(k);
        }
    }

    /// <summary>
    /// Clears all stored cursors for the current selector (useful at session start/reset).
    /// </summary>
    public void Clear()
    {
        lock (_gate)
        {
            _cursors.Clear();
        }
    }

    /// <summary>
    /// Computes x mod m, ensuring a non-negative result even when x is negative.
    /// </summary>
    private static int Mod(int x, int m) => ((x % m) + m) % m;

    /// <summary>
    /// Gets the next index for the given key and decomposition, then advances the cursor modulo <paramref name="count"/>.
    /// </summary>
    private int GetAndAdvanceIndex(string key, int decompIndex, int count)
    {
        (string key, int decompIndex) k = (key, decompIndex);
        lock (_gate)
        {
            if (!_cursors.TryGetValue(k, out int next))
            {
                next = 0;
            }

            int selected = Mod(next, count);
            _cursors[k] = Mod(selected + 1, count);
            return selected;
        }
    }

    /// <summary>
    /// Provides value equality for the <c>(string key, int decompIndex)</c> dictionary key,
    /// honoring the configured string comparer.
    /// </summary>
    private sealed class KeyTupleComparer : IEqualityComparer<(string key, int decompIndex)>
    {
        private readonly IEqualityComparer<string> _cmp;

        public KeyTupleComparer(IEqualityComparer<string> cmp) => _cmp = cmp;

        public bool Equals((string key, int decompIndex) x, (string key, int decompIndex) y) =>
            _cmp.Equals(x.key, y.key) && x.decompIndex == y.decompIndex;

        public int GetHashCode((string key, int decompIndex) obj)
        {
            unchecked
            {
                int h = 17;
                h = (h * 31) + _cmp.GetHashCode(obj.key);
                h = (h * 31) + obj.decompIndex.GetHashCode();
                return h;
            }
        }
    }
}
