// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;

namespace Genova.Eliza;

/// <summary>
/// Runtime engine for the 1966 ELIZA (DOCTOR) script that mirrors the original MAD–SLIP control flow
/// as closely as practical in C#.
/// </summary>
/// <remarks>
/// <para>
/// The engine implements the following behaviors to match the historical program:
/// </para>
/// <list type="bullet">
///   <item>
///     <description><b>Tokenization &amp; delimiters.</b> Input is upper-cased. The characters <c>?</c>, <c>!</c>,
///     and <c>;</c> are normalized to <c>.</c> prior to tokenization. During scanning, the delimiters
///     recognized are <c>.</c>, <c>,</c>, and the word <c>BUT</c>, matching the script’s expectations.
///     </description>
///   </item>
///   <item>
///     <description><b>Keyword scanning &amp; precedence.</b> Candidate keywords are identified per-token using a
///     32-bucket hash. If multiple candidates match, an explicit numeric precedence (if present) is used
///     to choose the winner; otherwise the first matching keyword is chosen.</description>
///   </item>
///   <item>
///     <description><b>TESTS (keyword substitution).</b> At keyword match time, an optional synonym substitution
///     (e.g., <c>(DREAMS = DREAM)</c>) is applied to the input token before further processing.
///     </description>
///   </item>
///   <item>
///     <description><b>Decompositions.</b> For the selected keyword, decompositions are tried in order. The engine
///     supports the historical forms: link-only (<c>=WORD</c>), pattern with reassemblies, and “PRE + link”
///     (assemble a preface, retokenize, then follow the link).</description>
///   </item>
///   <item>
///     <description><b>YMATCH &amp; ASSMBL.</b> Pattern matching is token-level with wildcard <c>"0"</c> (0+ tokens),
///     literal tokens, set tokens (single-token membership), and tag tokens (single-token membership in a tagged set).
///     Captures are recorded in encounter order (including literals) and are referenced in reassemblies by
///     1-based indices. Reassemblies are used round-robin per decomposition.</description>
///   </item>
///   <item>
///     <description><b>Links in reassemblies.</b> A chosen reassembly that begins with <c>=WORD</c> is treated as
///     a link to another keyword’s rules. The special token <c>NEWKEY</c> is treated as a directive, not output.
///     </description>
///   </item>
///   <item>
///     <description><b>MEMORY.</b> When the MEMORY keyword is matched, a memory line is created using one of the
///     four MEMORY rules selected by a 2-bit mid-square SLIP hash of the last non-delimiter word of the input.
///     Memories are recalled when the LIMIT counter reaches 4.</description>
///   </item>
///   <item>
///     <description><b>SLIP mid-square HASH.</b> A faithful mid-square routine (<see cref="HashN(ulong, int)"/>)
///     operates on a 36-bit packed word. Packing uses right-packing of up to six 6-bit codes per word
///     (<see cref="PackWord36_Right(string)"/>). <see cref="Hash4(string)"/> uses this for MEMORY selection,
///     and a separate simple hash is used for 32-bucket keyword bucketing.</description>
///   </item>
/// </list>
/// </remarks>
internal sealed partial class ElizaEngine
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules.")]
    private static readonly string[] _noMatch =
    [
        "PLEASE CONTINUE", "HMMM", "GO ON, PLEASE", "I SEE",
    ];

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules.")]
    private static readonly HashSet<string> _delims = new(StringComparer.Ordinal)
    { ".", ",", "BUT" };

    private readonly DoctorScript _script;
    private readonly Dictionary<string, KeywordEntry> _kwByWord;
    private readonly List<KeywordEntry>[] _buckets = new List<KeywordEntry>[32];
    private readonly Dictionary<(string Keyword, int DecompositionIndex), int> _reasmRotation = [];
    private readonly Queue<string> _memory = new();
    private readonly string _memoryKeyword;
    private readonly Dictionary<string, HashSet<string>> _tagMap;

    private int _limit = 1; // cycles 1..4
    private int _noneIndex = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElizaEngine"/> class using the given script.
    /// </summary>
    /// <param name="script">The parsed DOCTOR script containing greeting, NONE, MEMORY, and keyword rules.</param>
    public ElizaEngine(DoctorScript script)
    {
        _script = script;

        _kwByWord = new Dictionary<string, KeywordEntry>(StringComparer.Ordinal);
        foreach (KeywordEntry k in script.Keywords)
        {
            _kwByWord[k.Keyword] = k;
        }

        for (int i = 0; i < _buckets.Length; i++)
        {
            _buckets[i] = [];
        }

        foreach (KeywordEntry k in script.Keywords)
        {
            _buckets[Hash32(k.Keyword)].Add(k);
        }

        _memoryKeyword = script.Memory.Keyword;

        // TAG map from DLIST (TAG -> words having that tag)
        _tagMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (KeywordEntry k in script.Keywords)
        {
            if (k.DList is null)
            {
                continue;
            }

            foreach (string tag in k.DList)
            {
                if (!_tagMap.TryGetValue(tag, out HashSet<string>? set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    _tagMap[tag] = set;
                }

                set.Add(k.Keyword);
            }
        }
    }

    /// <summary>
    /// Produces one ELIZA reply for the given user input, applying the historical scanning,
    /// decomposition, reassembly, memory, and fallback behaviors.
    /// </summary>
    /// <param name="userInput">The raw user input line.</param>
    /// <returns>The response line generated by the engine.</returns>
    public string Reply(string userInput)
    {
        userInput = userInput.Replace("’", "'").Replace("?", ".").Replace("!", ".").Replace(";", ".");

        // LIMIT 1..4
        _limit++;
        if (_limit == 5)
        {
            _limit = 1;
        }

        List<string> tokens = Tokenize(userInput);
        if (tokens.Count == 0)
        {
            return NoneFallback();
        }

        // SCAN for keyword
        KeywordEntry? selectedKw = null;
        int? selectedPred = null;

        int i = 0;
        while (i < tokens.Count)
        {
            string w = tokens[i];

            if (_delims.Contains(w))
            {
                if (selectedKw is null)
                {
                    // NULSTL: discard left part including delimiter
                    tokens.RemoveRange(0, i + 1);
                    i = 0;
                    continue;
                }
                else
                {
                    // NULSTR: discard right part from delimiter
                    tokens.RemoveRange(i, tokens.Count - i);
                    break;
                }
            }

            List<KeywordEntry> bucket = _buckets[Hash32(w)];
            for (int j = 0; j < bucket.Count; j++)
            {
                KeywordEntry cand = bucket[j];
                if (!StringEquals(cand.Keyword, w))
                {
                    continue;
                }

                if (!Tests_Substitute(tokens, i, cand))
                {
                    continue;
                }

                if (selectedKw is null && cand.Precedence is null)
                {
                    selectedKw = cand;
                }
                else if (cand.Precedence is { } p)
                {
                    if (selectedPred is null || p > selectedPred.Value)
                    {
                        selectedPred = p;
                        selectedKw = cand;
                    }
                }
            }

            i++;
        }

        if (selectedKw is null)
        {
            if (_limit == 4 && _memory.Count > 0)
            {
                return _memory.Dequeue();
            }

            return NoneFallback();
        }

        // MEMORY keyword behavior: enqueue a memory line based on last word hash
        if (StringEquals(selectedKw.Keyword, _memoryKeyword))
        {
            // pick the last token that is NOT a delimiter (., , BUT)
            string last = "";
            for (int k = tokens.Count - 1; k >= 0; k--)
            {
                if (!_delims.Contains(tokens[k]))
                {
                    last = tokens[k];
                    break;
                }
            }

            int memIndex = Hash4(last); // 0..3
            MemoryRule memRule = _script.Memory.Rules[memIndex];

            string? memOut = AssembleFromRuleIfMatch(memRule, tokens);
            if (memOut is not null)
            {
                _memory.Enqueue(memOut);
            }
        }

        string? output = TryDecompositions(selectedKw, tokens);
        if (output is not null)
        {
            return output;
        }

        return _noMatch[(_limit - 1) % _noMatch.Length];
    }

    /// <summary>
    /// Computes a simple 32-bucket hash (0..31) for keyword bucketing over uppercase letters/digits.
    /// </summary>
    /// <param name="word">The word to hash.</param>
    /// <returns>An integer in the range 0..31 used for bucket selection.</returns>
    private static int Hash32(string word)
    {
        int h = 0;
        for (int i = 0; i < word.Length; i++)
        {
            char c = word[i];
            int v = MapChar(c);
            h = ((h << 3) ^ v) & 0x7FFFFFFF;
        }

        return h & 31;
    }

    /// <summary>
    /// Recreates SLIP <c>HASH.(D,N)</c>: returns an <paramref name="n"/>-bit mid-square hash value of a 36-bit datum.
    /// Only the least-significant 35 bits are squared (709x sign/magnitude).
    /// </summary>
    /// <param name="d">The packed 36-bit datum, stored in a 64-bit unsigned integer.</param>
    /// <param name="n">The number of bits to return (0..15 inclusive).</param>
    /// <returns>A value in the range <c>0..(2^n-1)</c>.</returns>
    private static int HashN(ulong d, int n)
    {
        // Mask off to 35-bit magnitude (clear the 709x sign bit).
        d &= 0x7FFFFFFFFUL;                       // 35 bits of 1s

        // Square in 64-bit, wraparound allowed (original produced 70-bit AC/MQ).
        unchecked
        {
            d *= d;
        }

        // Shift the middle n bits down to LSBs.
        // (Matches: shift by 35 - floor(n/2), per the FAP notes.)
        d >>= 35 - (n / 2);

        // Keep only n bits.
        return (int)(d & ((1UL << n) - 1));
    }

    /// <summary>
    /// Right-packing helper: first character ends in the bottom 6 bits of the 36-bit word.
    /// This packing is used with <see cref="HashN(ulong, int)"/> for MEMORY selection.
    /// </summary>
    /// <param name="word">The word to pack.</param>
    /// <returns>A 36-bit datum in a 64-bit unsigned integer.</returns>
    private static ulong PackWord36_Right(string word)
    {
        string s = word.ToUpperInvariant();
        ulong acc = 0;
        int count = 0;
        foreach (char c in s)
        {
            int v = Map6(c);
            if (v == 0)
            {
                continue;
            }

            acc |= ((ulong)(v & 0x3F)) << (6 * count); // fill from LSB upwards
            if (++count == 6)
            {
                break;
            }
        }

        // If fewer than 6 chars, still fine; keep to 36 bits:
        return acc & 0xFFFFFFFFFUL;
    }

    /// <summary>
    /// Maps a single character to a 6-bit code (A..Z → 1..26, 0..9 → 27..36); returns 0 for others.
    /// </summary>
    /// <param name="c">The character to map.</param>
    /// <returns>The 6-bit code, or 0 if unmapped.</returns>
    private static int Map6(char c)
    {
        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 1;      // 1..26
        }

        if (c >= '0' && c <= '9')
        {
            return c - '0' + 27;     // 27..36
        }

        return 0; // ignore punctuation/spaces
    }

    /// <summary>
    /// SLIP-style 2-bit hash (0..3) for MEMORY rule selection: packs the word via
    /// <see cref="PackWord36_Right(string)"/> and returns <c>HASH.(D,2)</c>.
    /// </summary>
    /// <param name="word">The last non-delimiter word of the input.</param>
    /// <returns>An integer in the range 0..3.</returns>
    private static int Hash4(string word)
    {
        int hash = HashN(PackWord36_Right(word), 2);
        return hash;
    }

    /// <summary>
    /// Maps a character to a small integer for the bucket hash: A..Z → 1..26, 0..9 → 27..36, otherwise 0.
    /// </summary>
    /// <param name="c">The character to map.</param>
    /// <returns>The mapping result.</returns>
    private static int MapChar(char c)
    {
        if (c >= 'A' && c <= 'Z')
        {
            return c - 'A' + 1;
        }

        if (c >= '0' && c <= '9')
        {
            return c - '0' + 27;
        }

        return 0;
    }

    /// <summary>
    /// Performs the historical keyword equality check and applies keyword-level substitution if defined.
    /// </summary>
    /// <param name="tokens">The token list (modified in place if substitution applies).</param>
    /// <param name="index">The index of the candidate token in <paramref name="tokens"/>.</param>
    /// <param name="cand">The candidate keyword entry.</param>
    /// <returns><see langword="true"/> if the token equals the keyword (after which substitution may be applied); otherwise <see langword="false"/>.</returns>
    private static bool Tests_Substitute(List<string> tokens, int index, KeywordEntry cand)
    {
        if (!StringEquals(tokens[index], cand.Keyword))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(cand.Substitution))
        {
            tokens[index] = cand.Substitution!;
        }

        return true;
    }

    /// <summary>
    /// Assembles a response by substituting 1-digit placeholders (1..9) with the corresponding captured segments.
    /// Performs minor whitespace cleanup before punctuation.
    /// </summary>
    /// <param name="reassembly">The reassembly template.</param>
    /// <param name="caps">The capture list (1-based indexing in the template).</param>
    /// <returns>The assembled response string.</returns>
    private static string AssembleOne(string reassembly, List<string> caps)
    {
        // Replace stand-alone digits 1..9 with capture 1-based indexing
        string res = StandaloneDigits().Replace(reassembly, m =>
        {
            int idx = m.Groups[1].Value[0] - '0';
            int capIndex = idx - 1;
            if (capIndex >= 0 && capIndex < caps.Count)
            {
                return caps[capIndex];
            }

            return m.Value;
        });

        // Tidy whitespace before punctuation
        res = SpaceBeforePunctuation().Replace(res, "$1");
        res = MultipleWhitespaces().Replace(res, " ").Trim();
        return res;
    }

    /// <summary>
    /// Tokenizes the input into uppercase tokens, spacing <c>.</c> and <c>,</c> as standalone tokens.
    /// </summary>
    /// <param name="text">The raw input text.</param>
    /// <returns>The list of tokens.</returns>
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Resolve conflict with SA1018")]
    private static List<string> Tokenize(string text)
    {
        string s = text.ToUpperInvariant();
        s = s.Replace(".", " . ").Replace(",", " , ");
        string[] raw = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        List<string> tokens = new(raw.Length);
        foreach (string t in raw)
        {
            string tt = t.Trim();
            if (tt.Length == 0)
            {
                continue;
            }

            tokens.Add(tt);
        }

        return tokens;
    }

    /// <summary>
    /// Joins a span of tokens with spaces.
    /// </summary>
    /// <param name="tokens">The token list.</param>
    /// <param name="start">The start index (inclusive).</param>
    /// <param name="len">The number of tokens to join.</param>
    /// <returns>The joined string, or an empty string if <paramref name="len"/> is 0 or negative.</returns>
    private static string Join(List<string> tokens, int start, int len)
    {
        if (len <= 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new();
        for (int k = 0; k < len; k++)
        {
            if (k > 0)
            {
                sb.Append(' ');
            }

            sb.Append(tokens[start + k]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Performs ordinal string equality.
    /// </summary>
    /// <param name="a">First string.</param>
    /// <param name="b">Second string.</param>
    /// <returns><see langword="true"/> if equal; otherwise <see langword="false"/>.</returns>
    private static bool StringEquals(string a, string b)
    {
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    [GeneratedRegex(@"\b([0-9])\b")]
    private static partial Regex StandaloneDigits();

    [GeneratedRegex(@"\s+([,.!?])")]
    private static partial Regex SpaceBeforePunctuation();

    [GeneratedRegex(@"\s{2,}")]
    private static partial Regex MultipleWhitespaces();

    /// <summary>
    /// Tries the decompositions for the selected keyword in order, evaluating link-only, PRE+link, and
    /// pattern-with-reassembly forms. Returns the first successfully assembled response, or <see langword="null"/>
    /// if none match.
    /// </summary>
    /// <param name="kw">The selected keyword entry.</param>
    /// <param name="inputTokens">The current input tokens.</param>
    /// <returns>The response string to output, or <see langword="null"/> to indicate no match.</returns>
    private string? TryDecompositions(KeywordEntry kw, List<string> inputTokens)
    {
        for (int di = 0; di < kw.Decompositions.Count; di++)
        {
            Decomposition d = kw.Decompositions[di];

            // Link-only
            if (!string.IsNullOrWhiteSpace(d.Link) && (d.Pattern is null || d.Pattern.Count == 0))
            {
                string? linkOut = FollowLink(d.Link!, inputTokens);
                if (linkOut is not null)
                {
                    return linkOut;
                }

                continue;
            }

            if (d.Pattern is null || d.Pattern.Count == 0)
            {
                continue;
            }

            if (!YMatch(d.Pattern, inputTokens, out List<string>? captures))
            {
                continue;
            }

            // PRE + LINK (R5)
            if (!string.IsNullOrWhiteSpace(d.Link))
            {
                List<string> virtualInput = inputTokens;
                if (!string.IsNullOrWhiteSpace(d.Pre))
                {
                    string? preLine = AssembleOne(d.Pre!, captures);
                    virtualInput = Tokenize(preLine);
                }

                string? linkOut = FollowLink(d.Link!, virtualInput);
                if (linkOut is not null)
                {
                    return linkOut;
                }

                continue;
            }

            if (d.Reassembly is null || d.Reassembly.Count == 0)
            {
                continue;
            }

            // Round-robin reassembly choice
            int rot = 0;
            var key = (Keyword: kw.Keyword, DecompositionIndex: di);
            if (_reasmRotation.TryGetValue(key, out int last))
            {
                rot = last;
            }

            string re = d.Reassembly[rot % d.Reassembly.Count];
            _reasmRotation[key] = (rot + 1) % d.Reassembly.Count;

            // NEW: handle special reassembly directives
            if (string.Equals(re, "NEWKEY", StringComparison.Ordinal))
            {
                // Do not print; let caller move on to fallback.
                return null;
            }

            if (re.Length > 1 && re[0] == '=')
            {
                string linkKw = re.Substring(1);
                string? linkOut = FollowLink(linkKw, inputTokens);
                if (linkOut is not null)
                {
                    return linkOut;
                }

                continue;
            }

            string outLine = AssembleOne(re, captures);
            return outLine;
        }

        return null;
    }

    /// <summary>
    /// Follows a link to another keyword and attempts its decompositions with the given tokens.
    /// </summary>
    /// <param name="linkKeyword">The target keyword to follow.</param>
    /// <param name="inputTokens">The (possibly retokenized) input tokens.</param>
    /// <returns>The response string to output, or <see langword="null"/> if no decomposition matched.</returns>
    private string? FollowLink(string linkKeyword, List<string> inputTokens)
    {
        if (!_kwByWord.TryGetValue(linkKeyword, out KeywordEntry? target))
        {
            return null;
        }

        return TryDecompositions(target, inputTokens);
    }

    /// <summary>
    /// Attempts to match the given pattern against the input tokens using the historical rules:
    /// wildcard <c>"0"</c> captures 0+ tokens; literals, set tokens, and tag tokens capture one token.
    /// Captures are recorded in encounter order (including literals).
    /// </summary>
    /// <param name="pattern">The pattern tokens to match.</param>
    /// <param name="input">The input tokens.</param>
    /// <param name="caps">On success, receives the ordered list of captured segments.</param>
    /// <returns><see langword="true"/> if the pattern matches; otherwise <see langword="false"/>.</returns>
    private bool YMatch(List<PatternToken> pattern, List<string> input, out List<string> caps)
    {
        List<string> captures = [];

        bool ok = MatchFrom(0, 0);
        caps = captures;
        return ok;

        bool MatchFrom(int startInput, int startPat)
        {
            if (startPat == pattern.Count)
            {
                return true;
            }

            PatternToken tok = pattern[startPat];

            // wildcard "0" -> capture 0 or more tokens
            if (tok is StringToken st && st.Text == "0")
            {
                for (int len = input.Count - startInput; len >= 0; len--)
                {
                    string? span = Join(input, startInput, len);
                    captures.Add(span);
                    if (MatchFrom(startInput + len, startPat + 1))
                    {
                        return true;
                    }

                    captures.RemoveAt(captures.Count - 1);
                }

                return false;
            }

            // literal token
            if (tok is StringToken st2)
            {
                if (startInput >= input.Count)
                {
                    return false;
                }

                if (!StringEquals(st2.Text, input[startInput]))
                {
                    return false;
                }

                captures.Add(input[startInput]);
                bool okInner = MatchFrom(startInput + 1, startPat + 1);
                if (!okInner)
                {
                    captures.RemoveAt(captures.Count - 1);
                }

                return okInner;
            }

            // set token
            if (tok is SetToken setTok)
            {
                if (startInput >= input.Count)
                {
                    return false;
                }

                if (!setTok.Items.Contains(input[startInput]))
                {
                    return false;
                }

                captures.Add(input[startInput]);
                bool okInner = MatchFrom(startInput + 1, startPat + 1);
                if (!okInner)
                {
                    captures.RemoveAt(captures.Count - 1);
                }

                return okInner;
            }

            // tag token
            if (tok is TagToken tagTok)
            {
                if (startInput >= input.Count)
                {
                    return false;
                }

                bool match = false;
                foreach (string tag in tagTok.Tags)
                {
                    if (_tagMap.TryGetValue(tag, out HashSet<string>? set) && set.Contains(input[startInput]))
                    {
                        match = true;
                        break;
                    }
                }

                if (!match)
                {
                    return false;
                }

                captures.Add(input[startInput]);
                bool okInner = MatchFrom(startInput + 1, startPat + 1);
                if (!okInner)
                {
                    captures.RemoveAt(captures.Count - 1);
                }

                return okInner;
            }

            return false;
        }
    }

    /// <summary>
    /// Attempts to assemble a MEMORY response from a MEMORY rule if the rule's pattern matches the input tokens.
    /// </summary>
    /// <param name="rule">The MEMORY rule to try.</param>
    /// <param name="inputTokens">The current input tokens.</param>
    /// <returns>The assembled memory line, or <see langword="null"/> if the rule did not match.</returns>
    private string? AssembleFromRuleIfMatch(MemoryRule rule, List<string> inputTokens)
    {
        if (!YMatch(rule.Pattern, inputTokens, out List<string>? caps))
        {
            return null;
        }

        return AssembleOne(rule.Reassembly, caps);
    }

    /// <summary>
    /// Returns a NONE fallback response, cycling through the configured list.
    /// </summary>
    /// <returns>The fallback response string.</returns>
    private string NoneFallback()
    {
        if (_script.None.Count == 0)
        {
            return _noMatch[(_limit - 1) % _noMatch.Length];
        }

        string s = _script.None[_noneIndex % _script.None.Count];
        _noneIndex++;
        return s;
    }
}
