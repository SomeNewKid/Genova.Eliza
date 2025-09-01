// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Matches decomposition patterns against preprocessed input tokens and produces
/// capture groups compatible with ELIZA reassembly templates.
/// <para>
/// Pattern tokens supported:
/// <list type="bullet">
/// <item><description><see cref="LiteralToken"/>: exact token match (case-insensitive).</description></item>
/// <item><description><see cref="WildcardToken"/>: matches zero or more tokens (greedy with backtracking).</description></item>
/// <item><description><see cref="AltsToken"/>: matches any one of the listed literals (case-insensitive).</description></item>
/// <item><description><see cref="TagToken"/>: matches a token bearing the specified lexicon tag.</description></item>
/// </list>
/// Captures are 1-based (i.e., <c>$1</c> corresponds to the first pattern token, <c>$2</c> to the second, etc.).
/// For <see cref="WildcardToken"/>, the capture is the space-joined span it matched (possibly empty).
/// For literal/alternation/tag tokens, the capture is the actual input token matched.
/// </para>
/// </summary>
internal sealed class PatternMatcher
{
    private readonly DoctorScript _script;
    private readonly StringComparer _cmp;
    private readonly StringComparer _tagCmp;

    /// <summary>
    /// Initializes a new instance of the <see cref="PatternMatcher"/> class.
    /// </summary>
    /// <param name="script">The loaded <see cref="DoctorScript"/> used for tag lookups.</param>
    /// <param name="caseInsensitive">If <c>true</c>, matching is performed case-insensitively.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="script"/> is null.</exception>
    public PatternMatcher(DoctorScript script, bool caseInsensitive = true)
    {
        _script = script ?? throw new ArgumentNullException(nameof(script));
        _cmp = caseInsensitive ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        _tagCmp = StringComparer.OrdinalIgnoreCase;
    }

    /// <summary>
    /// Attempts to match a decomposition’s pattern against the provided tokens.
    /// </summary>
    /// <param name="tokens">Preprocessed input tokens (after simple/pre substitutions).</param>
    /// <param name="decomposition">The decomposition whose pattern to match.</param>
    /// <param name="captures">
    /// On success, receives a 1-based capture list suitable for template rendering
    /// (index <c>1</c> is the first capture). On failure, set to an empty list.
    /// </param>
    /// <returns><c>true</c> if the pattern matches; otherwise <c>false</c>.</returns>
    public bool TryMatch(IReadOnlyList<string> tokens, Decomposition decomposition, out List<string> captures)
    {
        ArgumentNullException.ThrowIfNull(decomposition);
        return TryMatch(tokens, decomposition.Pattern, out captures);
    }

    /// <summary>
    /// Attempts to match a pattern token sequence against the provided tokens.
    /// </summary>
    /// <param name="tokens">Preprocessed input tokens (after simple/pre substitutions).</param>
    /// <param name="pattern">The sequence of pattern tokens.</param>
    /// <param name="captures">
    /// On success, receives a 1-based capture list suitable for template rendering
    /// (index <c>1</c> is the first capture). On failure, set to an empty list.
    /// </param>
    /// <returns><c>true</c> if the pattern matches; otherwise <c>false</c>.</returns>
    public bool TryMatch(IReadOnlyList<string> tokens, IList<PatternToken> pattern, out List<string> captures)
    {
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(tokens);
        captures = new List<string>(capacity: (pattern?.Count ?? 0) + 1) { string.Empty }; // index 0 unused

        // Prepare a buffer to hold per-token captures (0-based for pattern index; later we offset to 1-based).
        string[] segments = new string[pattern!.Count];
        bool success = MatchRecursive(tokens, 0, pattern, 0, segments);

        if (!success)
        {
            captures.Clear();
            return false;
        }

        // Build 1-based captures: $1..$N correspond to pattern tokens 0..N-1.
        captures.AddRange(segments);
        return true;
    }

    /// <summary>
    /// Joins tokens in the range [<paramref name="start"/>, <paramref name="end"/>)
    /// with a single space; returns an empty string if the range is empty.
    /// </summary>
    private static string Join(IReadOnlyList<string> tokens, int start, int end)
    {
        int count = end - start;
        if (count <= 0)
        {
            return string.Empty;
        }

        if (count == 1)
        {
            return tokens[start];
        }

        // Efficient join without creating intermediate arrays.
        string[] parts = new string[count];
        for (int i = 0; i < count; i++)
        {
            parts[i] = tokens[start + i];
        }

        return string.Join(' ', parts);
    }

    /// <summary>
    /// Recursively matches pattern tokens to the input, performing greedy wildcard
    /// matching with backtracking when necessary.
    /// </summary>
    private bool MatchRecursive(
        IReadOnlyList<string> tokens,
        int ti,            // current token index
        IList<PatternToken> pattern,
        int pi,            // current pattern index
        string[] segments) // per-pattern capture buffer
    {
        // End cases
        if (pi >= pattern.Count)
        {
            // Pattern consumed ⇒ success only if all tokens also consumed
            return ti == tokens.Count;
        }

        PatternToken tokenKind = pattern[pi];

        // Wildcard: try the longest possible span and back off until match succeeds.
        if (tokenKind is WildcardToken)
        {
            // If this is the last pattern token, it matches the rest unconditionally.
            if (pi == pattern.Count - 1)
            {
                segments[pi] = Join(tokens, ti, tokens.Count);
                return true;
            }

            // Otherwise, try greedily from the end back to current ti.
            for (int end = tokens.Count; end >= ti; end--)
            {
                segments[pi] = Join(tokens, ti, end);
                if (MatchRecursive(tokens, end, pattern, pi + 1, segments))
                {
                    return true;
                }
            }

            return false;
        }

        // For non-wildcards, we must have at least one token remaining.
        if (ti >= tokens.Count)
        {
            return false;
        }

        string current = tokens[ti];

        switch (tokenKind)
        {
            case LiteralToken lit:
            {
                if (!_cmp.Equals(current, lit.Value))
                {
                    return false;
                }

                segments[pi] = current; // capture actual input token
                return MatchRecursive(tokens, ti + 1, pattern, pi + 1, segments);
            }

            case AltsToken alts:
            {
                if (!alts.Alts.Any(a => _cmp.Equals(current, a)))
                {
                    return false;
                }

                segments[pi] = current; // capture actual input token
                return MatchRecursive(tokens, ti + 1, pattern, pi + 1, segments);
            }

            case TagToken tagTok:
            {
                if (!TokenHasTag(current, tagTok.Tag))
                {
                    return false;
                }

                segments[pi] = current; // capture actual input token
                return MatchRecursive(tokens, ti + 1, pattern, pi + 1, segments);
            }

            default:
            {
                // Unknown token type
                return false;
            }
        }
    }

    /// <summary>
    /// Determines whether a token is associated with the specified tag in the lexicon.
    /// </summary>
    /// <param name="token">The token to test.</param>
    /// <param name="tag">The tag name (case-insensitive).</param>
    /// <returns><c>true</c> if the token has the tag; otherwise <c>false</c>.</returns>
    private bool TokenHasTag(string token, string tag)
    {
        // Look up word in lexicon; keys may be in various casings, use case-insensitive search.
        // Prefer direct lookup with OrdinalIgnoreCase behavior.
        if (_script.Lexicon.TryGetValue(token, out LexEntry? entry))
        {
            return entry.Tags.Any(t => _tagCmp.Equals(t, tag));
        }

        // If the lexicon keys are normalized (e.g., lower-cased), fall back to a linear scan (rare).
        foreach (KeyValuePair<string, LexEntry> kvp in _script.Lexicon)
        {
            if (_cmp.Equals(kvp.Key, token))
            {
                return kvp.Value.Tags.Any(t => _tagCmp.Equals(t, tag));
            }
        }

        return false;
    }
}
