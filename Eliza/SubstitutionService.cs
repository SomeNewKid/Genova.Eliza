// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;
using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Provides case-insensitive application of the DOCTOR script’s substitution tables
/// to input tokens and generated output.
/// <para>
/// Pipeline guidance:
/// <list type="number">
/// <item><description>Apply <c>simple</c> substitutions (contractions/synonyms).</description></item>
/// <item><description>Apply optional lexicon-based substitutions (from <see cref="LexEntry.Substitution"/>).</description></item>
/// <item><description>Apply <c>pre</c> substitutions (persona/pronoun flips).</description></item>
/// <item><description>After reassembly, apply <c>post</c> substitutions to the final string.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed partial class SubstitutionService
{
    private readonly Dictionary<string, string> _simple;
    private readonly Dictionary<string, string> _pre;
    private readonly Dictionary<string, string> _post;
    private readonly Dictionary<string, string> _lexiconSubs; // word → canonical substitution (from lexicon)

    /// <summary>
    /// Initializes a new instance of the <see cref="SubstitutionService"/> class
    /// using the provided <see cref="DoctorScript"/> substitutions and lexicon.
    /// </summary>
    /// <param name="script">The loaded DOCTOR script.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="script"/> is null.</exception>
    public SubstitutionService(DoctorScript script)
    {
        ArgumentNullException.ThrowIfNull(script);

        _simple = ToCaseInsensitive(script.Substitutions.Simple);
        _pre = ToCaseInsensitive(script.Substitutions.Pre);
        _post = ToCaseInsensitive(script.Substitutions.Post);
        _lexiconSubs = BuildLexiconSubMap(script);
    }

    /// <summary>
    /// Gets a read-only view of the simple substitution map (case-insensitive keys).
    /// </summary>
    public IReadOnlyDictionary<string, string> SimpleMap => _simple;

    /// <summary>
    /// Gets a read-only view of the pre-substitution map (case-insensitive keys).
    /// </summary>
    public IReadOnlyDictionary<string, string> PreMap => _pre;

    /// <summary>
    /// Gets a read-only view of the post-substitution map (case-insensitive keys).
    /// </summary>
    public IReadOnlyDictionary<string, string> PostMap => _post;

    /// <summary>
    /// Applies the standard input preprocessing pipeline to the incoming tokens:
    /// <c>simple → lexicon (optional) → pre</c>, per token.
    /// </summary>
    /// <param name="tokens">The tokenized user input.</param>
    /// <returns>A new list of tokens after substitutions.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tokens"/> is null.</exception>
    public List<string> ProcessInputTokens(IReadOnlyList<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);

        List<string> result = new (tokens.Count);
        for (int i = 0; i < tokens.Count; i++)
        {
            string t = tokens[i];
            if (string.IsNullOrEmpty(t))
            {
                result.Add(t);
                continue;
            }

            // 1) simple (e.g., "dont" → "don't", "recollect" → "remember")
            t = ApplyMapToken(t, _simple);

            // 2) lexicon substitution (e.g., "mom" → "mother") if present
            t = ApplyMapToken(t, _lexiconSubs);

            // 3) pre (e.g., "i" → "you", "my" → "your")
            t = ApplyMapToken(t, _pre);

            result.Add(t);
        }

        return result;
    }

    /// <summary>
    /// Applies the post-substitution table to a completed response string on word boundaries,
    /// attempting to preserve original token casing.
    /// </summary>
    /// <param name="text">The response text to transform.</param>
    /// <returns>The transformed text.</returns>
    public string ApplyPostToText(string text)
    {
        if (string.IsNullOrEmpty(text) || _post.Count == 0)
        {
            return text ?? string.Empty;
        }

        return WordRegex().Replace(text, m =>
        {
            string original = m.Value;
            string key = original.ToLowerInvariant();

            if (!_post.TryGetValue(key, out string? replacement))
            {
                return original;
            }

            return PreserveCase(original, replacement);
        });
    }

    /// <summary>
    /// Applies the post-substitution table to a sequence of tokens.
    /// </summary>
    /// <param name="tokens">The token sequence to transform.</param>
    /// <returns>A new list of tokens after post substitutions.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="tokens"/> is null.</exception>
    public List<string> ApplyPostToTokens(IReadOnlyList<string> tokens)
    {
        ArgumentNullException.ThrowIfNull(tokens);
        List<string> result = new (tokens.Count);
        for (int i = 0; i < tokens.Count; i++)
        {
            string t = tokens[i];
            result.Add(ApplyMapToken(t, _post));
        }

        return result;
    }

    /// <summary>
    /// Applies a mapping to a single token using a case-insensitive key lookup.
    /// If no mapping exists, returns the token unchanged.
    /// </summary>
    private static string ApplyMapToken(string token, Dictionary<string, string> map)
    {
        if (map.Count == 0)
        {
            return token;
        }

        if (string.IsNullOrEmpty(token))
        {
            return token;
        }

        string key = token.ToLowerInvariant();
        return map.TryGetValue(key, out string? val) ? val : token;
    }

    /// <summary>
    /// Converts a dictionary to a case-insensitive dictionary with lower-cased keys.
    /// </summary>
    private static Dictionary<string, string> ToCaseInsensitive(Dictionary<string, string> src)
    {
        Dictionary<string, string> dst = new (StringComparer.OrdinalIgnoreCase);
        if (src is null)
        {
            return dst;
        }

        foreach (KeyValuePair<string, string> kvp in src)
        {
            if (string.IsNullOrEmpty(kvp.Key))
            {
                continue;
            }

            dst[kvp.Key.ToLowerInvariant()] = kvp.Value;
        }

        return dst;
    }

    /// <summary>
    /// Builds a case-insensitive map of lexicon-based substitutions from the script.
    /// (Only entries with a non-empty <see cref="LexEntry.Substitution"/> are included.)
    /// </summary>
    private static Dictionary<string, string> BuildLexiconSubMap(DoctorScript script)
    {
        Dictionary<string, string> map = new (StringComparer.OrdinalIgnoreCase);
        foreach (KeyValuePair<string, LexEntry> kvp in script.Lexicon)
        {
            string word = kvp.Key;
            LexEntry? entry = kvp.Value;
            if (!string.IsNullOrWhiteSpace(word) &&
                entry is not null &&
                !string.IsNullOrWhiteSpace(entry.Substitution))
            {
                map[word.ToLowerInvariant()] = entry.Substitution!;
            }
        }

        return map;
    }

    /// <summary>
    /// Attempts to preserve the casing of the original token in the replacement.
    /// </summary>
    private static string PreserveCase(string original, string replacement)
    {
        if (string.IsNullOrEmpty(original))
        {
            return replacement;
        }

        if (string.IsNullOrEmpty(replacement))
        {
            return replacement;
        }

        bool hasLetter = false, allUpper = true, allLower = true;
        foreach (char c in original)
        {
            if (char.IsLetter(c))
            {
                hasLetter = true;
                if (char.IsLower(c))
                {
                    allUpper = false;
                }

                if (char.IsUpper(c))
                {
                    allLower = false;
                }
            }
        }

        if (!hasLetter)
        {
            return replacement;
        }

        if (allUpper)
        {
            return replacement.ToUpperInvariant();
        }

        if (allLower)
        {
            return replacement.ToLowerInvariant();
        }

        // Title-ish: uppercase first char, lowercase the rest.
        if (char.IsLetter(original[0]) && char.IsUpper(original[0]))
        {
            if (replacement.Length == 1)
            {
                return replacement.ToUpperInvariant();
            }

            var sb = new StringBuilder(replacement.Length);
            sb.Append(char.ToUpperInvariant(replacement[0]));
            for (int i = 1; i < replacement.Length; i++)
            {
                sb.Append(char.ToLowerInvariant(replacement[i]));
            }

            return sb.ToString();
        }

        return replacement;
    }

    [GeneratedRegex(@"\b[\p{L}\p{M}]+(?:'[\p{L}\p{M}]+)?\b", RegexOptions.Compiled)]
    private static partial Regex WordRegex();
}
