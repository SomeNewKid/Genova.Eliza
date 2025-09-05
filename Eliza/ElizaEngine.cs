// ElizaEngine.cs
// C# engine for the 1966 ELIZA (DOCTOR) logic, designed to be as close as practical
// to the original MAD-SLIP program's behavior. This file implements the two
// SLIP-library-equivalent pieces called out earlier:
//   * HASH: bucket hashing (32 buckets) and 4-way hash for MEMORY rule selection
//   * YMATCH + ASSMBL: pattern decomposition and reassembly
//
// Dependencies: ElizaTyped.cs (or ElizaTyped_v2.cs) and a DOCTOR.json shaped for it.
//
// NOTE ON FIDELITY
// ----------------
// - Tokenization is UPPERCASE and uses delimiters '.', ',', and 'BUT' as in the source.
// - Keyword scanning, precedence selection, link handling, memory logic, NONE fallbacks,
//   and per-pattern round-robin reassemblies follow the original control flow.
// - YMATCH/ASSMBL are implemented at token level with support for:
//     • numeric placeholders ("0".."9")  -> wildcards that capture spans (or empty)
//     • set tokens {"set":[...]}         -> single-token membership match (captured)
//     • tag tokens {"tag":"BELIEF"}      -> single-token tag membership match (captured)
//   Captures are indexed in encounter order and interpolated into reassembly strings.
//   This matches the spirit of the original. If you need bit-for-bit transcript
//   reproduction, further tuning of capture numbering may be required for a few
//   patterns; the hooks are provided below.
//
// Build: .NET 6+
// Example usage (see bottom comment).

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Genova.Eliza;

internal sealed class ElizaEngine
{
    private readonly ElizaTyped _script;
    private readonly Dictionary<string, KeywordEntry> _kwByWord;
    private readonly List<KeywordEntry>[] _buckets = new List<KeywordEntry>[32];
    private readonly Dictionary<(string kw, int di), int> _reasmRotation = new();
    private readonly Queue<string> _memory = new();
    private readonly string _memoryKeyword;
    private readonly Dictionary<String, HashSet<string>> _tagMap;

    private int _limit = 1; // cycles 1..4
    private int _noneIndex = 0;

    private static readonly string[] _noMatch = new[]
    {
        "PLEASE CONTINUE", "HMMM", "GO ON , PLEASE", "I SEE"
    };

    private static readonly HashSet<string> _delims = new(StringComparer.Ordinal)
    { ".", ",", "BUT" };

    public ElizaEngine(ElizaTyped script)
    {
        _script = script;

        _kwByWord = new Dictionary<string, KeywordEntry>(StringComparer.Ordinal);
        foreach (var k in script.Keywords)
            _kwByWord[k.Keyword] = k;

        for (int i = 0; i < _buckets.Length; i++) _buckets[i] = new List<KeywordEntry>();
        foreach (var k in script.Keywords)
            _buckets[Hash32(k.Keyword)].Add(k);

        _memoryKeyword = script.Memory.Keyword;

        // TAG map from DLIST (TAG -> words having that tag)
        _tagMap = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var k in script.Keywords)
        {
            if (k.DList is null) continue;
            foreach (var tag in k.DList)
            {
                if (!_tagMap.TryGetValue(tag, out var set))
                {
                    set = new HashSet<string>(StringComparer.Ordinal);
                    _tagMap[tag] = set;
                }
                set.Add(k.Keyword);
            }
        }
    }

    public string Reply(string userInput)
    {
        userInput = userInput.Replace("?", ".").Replace("!", ".").Replace(";", ".");

        // LIMIT 1..4
        _limit++;
        if (_limit == 5) _limit = 1;

        var tokens = Tokenize(userInput);
        if (tokens.Count == 0)
            return NoneFallback();

        // SCAN for keyword
        KeywordEntry? selectedKw = null;
        int? selectedPred = null;

        int i = 0;
        while (i < tokens.Count)
        {
            var w = tokens[i];

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

            var bucket = _buckets[Hash32(w)];
            for (int j = 0; j < bucket.Count; j++)
            {
                var cand = bucket[j];
                if (!StringEquals(cand.Keyword, w)) continue;

                if (!Tests_Substitute(tokens, i, cand)) continue;

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
                return _memory.Dequeue();
            return NoneFallback();
        }

        // MEMORY keyword behavior: enqueue a memory line based on last word hash
        if (StringEquals(selectedKw.Keyword, _memoryKeyword))
        {
            // pick the last token that is NOT a delimiter (., , BUT)
            string last = "";
            for (int k = tokens.Count - 1; k >= 0; k--)
            {
                if (!_delims.Contains(tokens[k])) { last = tokens[k]; break; }
            }
            var idx = Hash4(last) % _script.Memory.Rules.Count;

            //var memRule = _script.Memory.Rules[idx];

            int memIndex = Hash4(last);            // 0..3
            var memRule = _script.Memory.Rules[memIndex];  // pick 1 of 4 deterministically

            var memOut = AssembleFromRuleIfMatch(memRule, tokens);
            if (memOut is not null) _memory.Enqueue(memOut);
        }

        var output = TryDecompositions(selectedKw, tokens);
        if (output is not null) return output;

        return _noMatch[(_limit - 1) % _noMatch.Length];
    }

    // ----- HASH (bucket + 4-way) -----
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
    /// Recreate SLIP HASH.(D,N): return an n-bit mid-square hash (0..(2^n-1))
    /// from a 36-bit datum D. Only the least-significant 35 bits of D are squared
    /// (709x sign+magnitude). n must be between 0 and 15 (inclusive).
    /// </summary>
    public static int HashN(ulong d, int n)
    {
        Debug.Assert(n >= 0 && n <= 15);

        // Mask off to 35-bit magnitude (clear the 709x sign bit).
        d &= 0x7FFFFFFFFUL;                       // 35 bits of 1s

        // Square in 64-bit, wraparound allowed (original produced 70-bit AC/MQ).
        unchecked { d = d * d; }

        // Shift the middle n bits down to LSBs.
        // (Matches: shift by 35 - floor(n/2), per the FAP notes.)
        d >>= (35 - (n / 2));

        // Keep only n bits.
        return (int)(d & ((1UL << n) - 1));
    }

    /// <summary>
    /// Pack up to 6 characters into a 36-bit datum using 6-bit codes:
    /// A..Z -> 1..26, 0..9 -> 27..36. Ignores spaces/punct. First char ends up in the top 6 bits.
    /// </summary>
    public static ulong PackWord36(string word)
    {
        if (word == null) return 0UL;

        var s = word.ToUpperInvariant();
        int count = 0;
        ulong acc = 0;

        for (int i = 0; i < s.Length && count < 6; i++)
        {
            char c = s[i];
            int v = 0;
            if (c >= 'A' && c <= 'Z') v = (c - 'A' + 1);         // 1..26
            else if (c >= '0' && c <= '9') v = (c - '0' + 27);   // 27..36
            else continue;                                       // skip others

            acc = ((acc << 6) | (ulong)(v & 0x3F)) & 0xFFFFFFFFFUL; // keep to 36 bits
            count++;
        }

        // If fewer than 6 codes, acc is still correctly positioned.
        return acc;
    }

    // A) Left-packed: first char ends up in the TOP 6 bits (current approach)
    public static ulong PackWord36_Left(string word)
    {
        var s = word.ToUpperInvariant();
        ulong acc = 0; int count = 0;
        foreach (char c in s)
        {
            int v = Map6(c); if (v == 0) continue;
            acc = ((acc << 6) | (ulong)(v & 0x3F)) & 0xFFFFFFFFFUL; // 36 bits
            if (++count == 6) break;
        }
        return acc;
    }

    // B) Right-packed: first char ends up in the BOTTOM 6 bits
    public static ulong PackWord36_Right(string word)
    {
        var s = word.ToUpperInvariant();
        ulong acc = 0; int count = 0;
        foreach (char c in s)
        {
            int v = Map6(c); if (v == 0) continue;
            acc |= ((ulong)(v & 0x3F)) << (6 * count); // fill from LSB upwards
            if (++count == 6) break;
        }
        // If fewer than 6 chars, still fine; keep to 36 bits:
        return acc & 0xFFFFFFFFFUL;
    }

    // Simple letter/digit mapping; swap this to a 709x BCD table if you have it
    private static int Map6(char c)
    {
        if (c >= 'A' && c <= 'Z') return (c - 'A' + 1);      // 1..26
        if (c >= '0' && c <= '9') return (c - '0' + 27);     // 27..36
        return 0; // ignore punctuation/spaces
    }


    /// <summary>
    /// HASH.(WORD, 2) — 4-way selection (0..3), using SLIP packing + mid-square.
    /// </summary>
    public static int Hash4(string word)
    {
        int hash = HashN(PackWord36_Right(word), 2);
        return hash;
    }

    private static int MapChar(char c)
    {
        if (c >= 'A' && c <= 'Z') return (c - 'A' + 1);
        if (c >= '0' && c <= '9') return (c - '0' + 27);
        return 0;
    }

    // ----- TESTS (full-word + substitution) -----
    private static bool Tests_Substitute(List<string> tokens, int index, KeywordEntry cand)
    {
        if (!StringEquals(tokens[index], cand.Keyword)) return false;
        if (!string.IsNullOrWhiteSpace(cand.Substitution))
            tokens[index] = cand.Substitution!;
        return true;
    }

    // ----- Decomposition selection -----
    private string? TryDecompositions(KeywordEntry kw, List<string> inputTokens)
    {
        for (int di = 0; di < kw.Decompositions.Count; di++)
        {
            var d = kw.Decompositions[di];

            // Link-only
            if (!string.IsNullOrWhiteSpace(d.Link) && (d.Pattern is null || d.Pattern.Count == 0))
            {
                var linkOut = FollowLink(d.Link!, inputTokens);
                if (linkOut is not null) return linkOut;
                continue;
            }

            if (d.Pattern is null || d.Pattern.Count == 0) continue;

            if (!YMatch(d.Pattern, inputTokens, out var captures)) continue;

            // PRE + LINK (R5)
            if (!string.IsNullOrWhiteSpace(d.Link))
            {
                var virtualInput = inputTokens;
                if (!string.IsNullOrWhiteSpace(d.Pre))
                {
                    var preLine = AssembleOne(d.Pre!, captures);
                    virtualInput = Tokenize(preLine);
                }
                var linkOut = FollowLink(d.Link!, virtualInput);
                if (linkOut is not null) return linkOut;
                continue;
            }

            if (d.Reassembly is null || d.Reassembly.Count == 0) continue;

            // Round-robin reassembly choice
            int rot = 0;
            var key = (kw.Keyword, di);
            if (_reasmRotation.TryGetValue(key, out var last)) rot = last;
            var re = d.Reassembly[rot % d.Reassembly.Count];
            _reasmRotation[key] = (rot + 1) % d.Reassembly.Count;

            // NEW: handle special reassembly directives
            if (string.Equals(re, "NEWKEY", StringComparison.Ordinal))
            {
                // Do not print; let caller move on to fallback.
                return null;
            }
            if (re.Length > 1 && re[0] == '=')
            {
                var linkKw = re.Substring(1);
                var linkOut = FollowLink(linkKw, inputTokens);
                if (linkOut is not null) return linkOut;
                continue;
            }

            var outLine = AssembleOne(re, captures);
            return outLine;
        }

        return null;
    }

    private string? FollowLink(string linkKeyword, List<string> inputTokens)
    {
        if (!_kwByWord.TryGetValue(linkKeyword, out var target))
            return null;
        return TryDecompositions(target, inputTokens);
    }

    // ----- YMATCH (wildcard "0" + literals + set/tag) -----
    private bool YMatch(List<PatternToken> pattern, List<string> input, out List<string> caps)
    {
        var captures = new List<string>();

        bool ok = MatchFrom(0, 0);
        caps = captures;
        return ok;

        bool MatchFrom(int startInput, int startPat)
        {
            if (startPat == pattern.Count)
            {
                return true;
            }

            var tok = pattern[startPat];

            // wildcard "0" -> capture 0 or more tokens
            if (tok is StringToken st && st.Text == "0")
            {
                for (int len = input.Count - startInput; len >= 0; len--)
                {
                    var span = Join(input, startInput, len);
                    captures.Add(span);
                    if (MatchFrom(startInput + len, startPat + 1)) return true;
                    captures.RemoveAt(captures.Count - 1);
                }
                return false;
            }

            // literal token
            if (tok is StringToken st2)
            {
                if (startInput >= input.Count) return false;
                if (!StringEquals(st2.Text, input[startInput])) return false;
                captures.Add(input[startInput]);
                var okInner = MatchFrom(startInput + 1, startPat + 1);
                if (!okInner) captures.RemoveAt(captures.Count - 1);
                return okInner;
            }

            // set token
            if (tok is SetToken setTok)
            {
                if (startInput >= input.Count) return false;
                if (!setTok.Items.Contains(input[startInput])) return false;
                captures.Add(input[startInput]);
                var okInner = MatchFrom(startInput + 1, startPat + 1);
                if (!okInner) captures.RemoveAt(captures.Count - 1);
                return okInner;
            }

            // tag token
            if (tok is TagToken tagTok)
            {
                if (startInput >= input.Count) return false;
                bool match = false;
                foreach (var tag in tagTok.Tags)
                {
                    if (_tagMap.TryGetValue(tag, out var set) && set.Contains(input[startInput]))
                    {
                        match = true; break;
                    }
                }
                if (!match) return false;
                captures.Add(input[startInput]);
                var okInner = MatchFrom(startInput + 1, startPat + 1);
                if (!okInner) captures.RemoveAt(captures.Count - 1);
                return okInner;
            }

            return false;
        }
    }

    // ----- ASSMBL -----
    private static string AssembleOne(string reassembly, List<string> caps)
    {
        // Replace stand-alone digits 1..9 with capture 1-based indexing
        var res = Regex.Replace(reassembly, @"\b([0-9])\b", m =>
        {
            int idx = m.Groups[1].Value[0] - '0';
            int capIndex = idx - 1;
            if (capIndex >= 0 && capIndex < caps.Count)
                return caps[capIndex];
            return m.Value;
        });

        // Tidy whitespace before punctuation
        res = Regex.Replace(res, @"\s+([,.!?])", "$1");
        res = Regex.Replace(res, @"\s{2,}", " ").Trim();
        return res;
    }

    private string? AssembleFromRuleIfMatch(MemoryRule rule, List<string> inputTokens)
    {
        if (!YMatch(rule.Pattern, inputTokens, out var caps))
            return null;
        return AssembleOne(rule.Reassembly, caps);
    }

    private string NoneFallback()
    {
        if (_script.None.Count == 0) return _noMatch[(_limit - 1) % _noMatch.Length];
        var s = _script.None[_noneIndex % _script.None.Count];
        _noneIndex++;
        return s;
    }

    private static List<string> Tokenize(string text)
    {
        var s = text.ToUpperInvariant();
        s = s.Replace(".", " . ").Replace(",", " , ");
        var raw = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var tokens = new List<string>(raw.Length);
        foreach (var t in raw)
        {
            var tt = t.Trim();
            if (tt.Length == 0) continue;
            tokens.Add(tt);
        }
        return tokens;
    }

    private static string Join(List<string> tokens, int start, int len)
    {
        if (len <= 0) return string.Empty;
        var sb = new StringBuilder();
        for (int k = 0; k < len; k++)
        {
            if (k > 0) sb.Append(' ');
            sb.Append(tokens[start + k]);
        }
        return sb.ToString();
    }

    private static bool StringEquals(string a, string b)
        => string.Equals(a, b, StringComparison.Ordinal);
}
