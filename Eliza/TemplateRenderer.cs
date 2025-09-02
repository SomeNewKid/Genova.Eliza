// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Genova.Eliza;

/// <summary>
/// Renders ELIZA reassembly templates into final reply strings.
/// <para>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Expand capture placeholders (e.g., <c>"$1"</c>, <c>"$2"</c>).</description></item>
/// <item><description>Optionally apply post-substitutions (pronoun flips) on word boundaries.</description></item>
/// <item><description>Optionally apply simple sentence casing and terminal punctuation fixes.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed partial class TemplateRenderer
{
    /// <summary>
    /// Renders a template using the given captures and options.
    /// </summary>
    /// <param name="template">The reassembly template (may include <c>$n</c> capture placeholders).</param>
    /// <param name="captures">A 1-based list of capture segments. Index 1 corresponds to <c>$1</c>.</param>
    /// <param name="postMap">Optional word-level post-substitution map (e.g., <c>"you" → "I"</c>).</param>
    /// <param name="applyPost">If <c>true</c>, applies <paramref name="postMap"/> after capture replacement.</param>
    /// <param name="sentenceCase">If <c>true</c>, applies a simple sentence-casing pass.</param>
    /// <param name="ensureTerminalPunctuation">If <c>true</c>, appends <c>'.'</c> when the result lacks terminal punctuation.</param>
    /// <returns>The rendered reply string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> or <paramref name="captures"/> is null.</exception>
    public static string Render(
        string template,
        IReadOnlyList<string> captures,
        IReadOnlyDictionary<string, string>? postMap = null,
        bool applyPost = true,
        bool sentenceCase = false,
        bool ensureTerminalPunctuation = false)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(captures);

        // Build a filtered post map that applies ONLY to captures and
        // intentionally does NOT flip "you" → "I" (or "you're" → "I'm") inside captures.
        // This preserves template phrasing like "You say $1?" → "You say you feel sad?"
        // while still allowing useful possessive flips like "your" → "my".
        Dictionary<string, string>? capturePostMap = null;
        if (applyPost && postMap is not null && postMap.Count > 0)
        {
            var filtered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> kv in postMap)
            {
                string k = kv.Key;
                if (string.Equals(k, "you", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (string.Equals(k, "you're", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                filtered[k] = kv.Value;
            }

            capturePostMap = filtered;
        }

        // 1) Preprocess captures ONLY (spacing; selective post on captures).
        var processedCaptures = new List<string>(captures.Count) { string.Empty }; // index 0 unused
        for (int i = 1; i < captures.Count; i++)
        {
            string seg = captures[i] ?? string.Empty;

            if (capturePostMap is not null && capturePostMap.Count > 0)
            {
                seg = ApplyWordMap(seg, capturePostMap);
            }

            seg = NormalizeEchoCapture(seg);
            processedCaptures.Add(seg);
        }

        // 2) Expand $n placeholders with processed captures.
        string expanded = CaptureRegex().Replace(template, m =>
        {
            if (!int.TryParse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int idx))
            {
                return m.Value;
            }

            return (idx >= 1 && idx < processedCaptures.Count) ? processedCaptures[idx] : string.Empty;
        });

        // 3) Normalize spacing/punctuation.
        expanded = SpaceFixRegex().Replace(expanded, "$1");
        expanded = NormalizeWhitespace(expanded);

        // 4) Presentation passes.
        if (sentenceCase)
        {
            expanded = ApplySentenceCase(expanded);
        }

        if (ensureTerminalPunctuation && !HasTerminalPunctuation(expanded))
        {
            expanded = expanded.TrimEnd() + ".";
        }

        return expanded;
    }

    /// <summary>
    /// Applies sentence casing to the text (capitalizes the first letter and trims spaces).
    /// Does not modify internal capitalization beyond the first leading letter.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <returns>The sentence-cased text.</returns>
    private static string ApplySentenceCase(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        text = text.Trim();

        // Find first letter to capitalize; keep any leading punctuation/spaces
        var sb = new StringBuilder(text.Length);
        bool capitalized = false;
        foreach (char ch in text)
        {
            if (!capitalized && char.IsLetter(ch))
            {
                sb.Append(char.ToUpper(ch, CultureInfo.InvariantCulture));
                capitalized = true;
            }
            else
            {
                sb.Append(ch);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Applies a word-level map (case-insensitive keys) on word boundaries,
    /// preserving the original token casing where reasonable.
    /// </summary>
    /// <param name="text">The input text.</param>
    /// <param name="map">The substitution map.</param>
    /// <returns>The mapped text.</returns>
    private static string ApplyWordMap(string text, Dictionary<string, string> map)
    {
        return WordRegex().Replace(text, m =>
        {
            string original = m.Value;
            string key = original.ToLowerInvariant();

            if (!map.TryGetValue(key, out string? replacement))
            {
                return original;
            }

            return PreserveCase(original, replacement);
        });
    }

    /// <summary>
    /// Attempts to preserve the casing style of the original token
    /// when replacing it with the provided text.
    /// </summary>
    /// <param name="original">The original token.</param>
    /// <param name="replacement">The replacement text.</param>
    /// <returns>The replacement text adjusted for casing.</returns>
    private static string PreserveCase(string original, string replacement)
    {
        if (string.IsNullOrEmpty(replacement))
        {
            return replacement;
        }

        bool hasLetter = false;
        bool allUpper = true;
        bool allLower = true;

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

        // Capitalized (Title) — only upper-case the first letter.
        // Preserve rest as-is (lowercase usually appropriate for pronouns).
        if (char.IsUpper(original[0]))
        {
            if (replacement.Length == 1)
            {
                return replacement.ToUpperInvariant();
            }

            return char.ToUpper(replacement[0], CultureInfo.InvariantCulture) + replacement.Substring(1).ToLowerInvariant();
        }

        return replacement;
    }

    /// <summary>
    /// Collapses repeated whitespace and trims.
    /// </summary>
    /// <param name="text">Input text.</param>
    /// <returns>Whitespace-normalized text.</returns>
    private static string NormalizeWhitespace(string text)
    {
        var sb = new StringBuilder(text.Length);
        bool lastSpace = false;
        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastSpace)
                {
                    sb.Append(' ');
                    lastSpace = true;
                }
            }
            else
            {
                sb.Append(c);
                lastSpace = false;
            }
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Determines whether the text ends with terminal punctuation (<c>.</c>, <c>!</c>, or <c>?</c>).
    /// Ignores trailing whitespace.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns><c>true</c> if the text ends with terminal punctuation; otherwise <c>false</c>.</returns>
    private static bool HasTerminalPunctuation(string text)
    {
        for (int i = text.Length - 1; i >= 0; i--)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            return c is '.' or '!' or '?';
        }

        return false;
    }

    private static string NormalizeEchoCapture(string seg)
    {
        // normalize spacing first
        seg = SpaceFixRegex().Replace(seg, "$1");
        seg = NormalizeWhitespace(seg);

        // lower-case the whole capture for natural echoing
        seg = seg.ToLowerInvariant();

        // restore first-person pronoun capitalization: i / i'm / i'd / i've / i'll
        seg = LowercaseIForms().Replace(seg, m =>
        {
            string suffix = m.Groups[1].Value; // includes "'m", "'d", "'ve", "'ll", or empty
            return "I" + suffix;
        });

        return seg;
    }

    [GeneratedRegex(@"\$(\d+)", RegexOptions.Compiled)]
    private static partial Regex CaptureRegex();

    [GeneratedRegex(@"\b[\p{L}\p{M}]+(?:'[\p{L}\p{M}]+)?\b", RegexOptions.Compiled)]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"\s+([,.;:!?])", RegexOptions.Compiled)]
    private static partial Regex SpaceFixRegex();

    [GeneratedRegex(@"(?<=^|\W)i((?:['\u2019](?:m|d|ve|ll))?)(?=\W|$)", RegexOptions.Compiled)]
    private static partial Regex LowercaseIForms();
}
