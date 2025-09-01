// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.RegularExpressions;

namespace Genova.Eliza;

/// <summary>
/// Splits user input into tokens for ELIZA processing.
/// <para>
/// Responsibilities:
/// <list type="bullet">
/// <item><description>Unicode normalization of quotes/dashes to ASCII forms (optional).</description></item>
/// <item><description>Extraction of word and number tokens; apostrophes within words are preserved.</description></item>
/// <item><description>Optional retention of punctuation as standalone tokens.</description></item>
/// <item><description>Optional casing normalization of tokens.</description></item>
/// </list>
/// </para>
/// </summary>
internal sealed partial class Tokenizer
{
    /// <summary>
    /// Gets or sets a value indicating whether to map curly quotes/typographic apostrophes
    /// and dashes to ASCII equivalents before tokenization. Defaults to <c>true</c>.
    /// </summary>
    public bool NormalizeUnicode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to include punctuation tokens
    /// (e.g., <c>"."</c>, <c>"?"</c>) in the output list. Defaults to <c>false</c>.
    /// </summary>
    public bool KeepPunctuationTokens { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to convert tokens to lower-case.
    /// Defaults to <c>false</c>. ELIZA matching is typically case-insensitive,
    /// so leaving tokens as-is is usually fine.
    /// </summary>
    public bool LowercaseTokens { get; set; } = false;

    /// <summary>
    /// Normalizes typographic characters to ASCII-friendly forms and collapses trivial whitespace.
    /// <para>
    /// Replacements:
    /// <list type="bullet">
    /// <item><description>Curly apostrophes/quotes: <c>’ ' , “ ” " , ‘</c> → <c>'</c> or <c>"</c></description></item>
    /// <item><description>En/em dashes: <c>– —</c> → <c>-</c></description></item>
    /// <item><description>Ellipsis: <c>…</c> → <c>...</c></description></item>
    /// </list>
    /// </para>
    /// </summary>
    /// <param name="text">The input text to normalize.</param>
    /// <returns>Normalized text.</returns>
    public static string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        // Replace common typographic variants with ASCII equivalents.
        // Order matters (handle quotes before removing stray spaces).
        text = text
            .Replace('\u2019', '\'') // right single quote (apostrophe)
            .Replace('\u2018', '\'') // left single quote
            .Replace('\u201C', '\"') // left double quote
            .Replace('\u201D', '\"') // right double quote
            .Replace('\u2013', '-') // en dash
            .Replace('\u2014', '-') // em dash
            .Replace('\u00A0', ' ') // non-breaking space
            .Replace('\u2026'.ToString(), "..."); // ellipsis

        // Collapse repeated whitespace
        return CollapseWhitespace(text);
    }

    /// <summary>
    /// Determines whether the provided token is a punctuation token recognized by this tokenizer.
    /// </summary>
    /// <param name="token">The token to test.</param>
    /// <returns><c>true</c> if the token is punctuation; otherwise <c>false</c>.</returns>
    public static bool IsPunctuationToken(string token)
    {
        if (string.IsNullOrEmpty(token) || token.Length != 1)
        {
            return false;
        }

        return token is "." or "?" or "!" or "," or ";" or ":";
    }

    /// <summary>
    /// Tokenizes the supplied input string into a sequence of surface tokens suitable
    /// for downstream preprocessing (substitutions) and pattern matching.
    /// </summary>
    /// <param name="input">The raw user input text.</param>
    /// <returns>A list of tokens (possibly empty if the input is null/whitespace).</returns>
    public List<string> Tokenize(string? input)
    {
        var tokens = new List<string>();
        if (string.IsNullOrWhiteSpace(input))
        {
            return tokens;
        }

        string text = NormalizeUnicode ? NormalizeText(input) : input;

        foreach (Match m in TokenRegex().Matches(text))
        {
            string t = m.Value;

            if (!KeepPunctuationTokens && IsPunctuationToken(t))
            {
                continue;
            }

            if (LowercaseTokens)
            {
                t = t.ToLowerInvariant();
            }

            tokens.Add(t);
        }

        return tokens;
    }

    private static string CollapseWhitespace(string text)
    {
        // Manual collapse avoids regex overhead on short inputs.
        List<char> chars = new (text.Length);
        bool lastSpace = false;

        foreach (char c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                if (!lastSpace)
                {
                    chars.Add(' ');
                    lastSpace = true;
                }
            }
            else
            {
                chars.Add(c);
                lastSpace = false;
            }
        }

        // Trim leading/trailing spaces introduced by collapse
        int start = 0;
        while (start < chars.Count && chars[start] == ' ')
        {
            start++;
        }

        int end = chars.Count - 1;
        while (end >= start && chars[end] == ' ')
        {
            end--;
        }

        return start > end ? string.Empty : new string(chars.GetRange(start, end - start + 1).ToArray());
    }

    // Matches:
    //  - words with optional internal apostrophes: you're, don't, it's
    //  - numbers (basic): 123 or 12.34
    //  - single-character punctuation tokens of interest: . ? ! , ; :
    [GeneratedRegex(@"[\p{L}\p{M}]+(?:'[\p{L}\p{M}]+)*|\d+(?:\.\d+)?|[.?!,;:]", RegexOptions.Compiled)]
    private static partial Regex TokenRegex();
}
