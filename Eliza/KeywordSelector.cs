// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Selects candidate keywords from the loaded <see cref="DoctorScript"/> that are
/// present in the user’s (preprocessed) input tokens and returns them ordered
/// by precedence (rank) and script order for tie-breaking.
/// <para>
/// Matching is case-insensitive by default. A keyword is considered present if
/// any of its trigger terms appear in the input. By default, the trigger terms
/// are the keyword’s own <see cref="Keyword.Key"/> and, if specified, the
/// <see cref="Keyword.Substitution"/> (e.g., <c>MY = YOUR</c> allows “YOUR” to
/// trigger the <c>MY</c> rule after pre-substitution).
/// </para>
/// </summary>
internal sealed class KeywordSelector
{
    private readonly DoctorScript _script;
    private readonly StringComparer _cmp;
    private readonly List<Keyword> _keywords; // preserves script order

    /// <summary>
    /// Initializes a new instance of the <see cref="KeywordSelector"/> class.
    /// </summary>
    /// <param name="script">The loaded DOCTOR script.</param>
    /// <param name="caseInsensitive">If <c>true</c>, matching is case-insensitive.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="script"/> is null.</exception>
    public KeywordSelector(DoctorScript script, bool caseInsensitive = true)
    {
        _script = script ?? throw new ArgumentNullException(nameof(script));
        _cmp = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        _keywords = _script.Keywords;
    }

    /// <summary>
    /// Finds all keywords whose trigger terms occur in the given token sequence,
    /// and returns them sorted by descending rank and then by script order.
    /// </summary>
    /// <param name="tokens">The preprocessed input tokens (after simple/pre substitutions).</param>
    /// <returns>An ordered list of matching <see cref="Keyword"/> entries.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tokens"/> is null.</exception>
    public List<Keyword> FindCandidates(IReadOnlyList<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        // Index tokens for O(1) membership tests with configured comparer.
        HashSet<string> tokenSet = new (tokens, _cmp);

        // Track matches while preserving first-found (script) order for tie-breaks.
        var matches = new List<(Keyword kw, int order, int rank)>(_keywords.Count);

        for (int i = 0; i < _keywords.Count; i++)
        {
            Keyword kw = _keywords[i];
            if (IsTriggeredBy(kw, tokenSet))
            {
                int rank = kw.Rank ?? 0;
                matches.Add((kw, i, rank));
            }
        }

        // Sort by rank desc, then by script order asc.
        matches.Sort((a, b) =>
        {
            int byRank = b.rank.CompareTo(a.rank);
            if (byRank != 0)
            {
                return byRank;
            }

            return a.order.CompareTo(b.order);
        });

        // Return keywords only (deduplicated just in case).
        HashSet<Keyword> seen = [];
        List<Keyword> result = new (matches.Count);
        foreach ((Keyword kw, int order, int rank) in matches)
        {
            if (seen.Add(kw))
            {
                result.Add(kw);
            }
        }

        return result;
    }

    /// <summary>
    /// Returns the single best keyword candidate according to rank and script order,
    /// or <c>null</c> if no keywords are present in the input.
    /// </summary>
    /// <param name="tokens">The preprocessed input tokens (after simple/pre substitutions).</param>
    /// <returns>The best matching <see cref="Keyword"/>, or <c>null</c> if none match.</returns>
    public Keyword? FindBest(IReadOnlyList<string> tokens)
    {
        List<Keyword> list = FindCandidates(tokens);
        return list.Count > 0 ? list[0] : null;
    }

    /// <summary>
    /// Determines whether a keyword is triggered by the current input, considering
    /// its key and (if present) its substitution value as trigger terms.
    /// </summary>
    private static bool IsTriggeredBy(Keyword kw, HashSet<string> tokenSet)
    {
        // Primary trigger: the keyword's own key.
        if (!string.IsNullOrWhiteSpace(kw.Key) && tokenSet.Contains(kw.Key))
        {
            return true;
        }

        // Secondary trigger: a keyword-level substitution (e.g., MY = YOUR).
        if (!string.IsNullOrWhiteSpace(kw.Substitution) && tokenSet.Contains(kw.Substitution))
        {
            return true;
        }

        return false;
    }
}
