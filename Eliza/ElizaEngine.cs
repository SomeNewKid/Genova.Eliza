// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;
using Genova.Eliza.Enums;
using Genova.Eliza.Models;
using Microsoft.AspNetCore.Components.RenderTree;

namespace Genova.Eliza;

/// <summary>
/// Orchestrates a full ELIZA turn using a loaded <see cref="DoctorScript"/>.
/// <para>
/// Pipeline per turn:
/// <list type="number">
/// <item><description>Tokenize input.</description></item>
/// <item><description>Apply <c>simple</c> + lexicon + <c>pre</c> substitutions.</description></item>
/// <item><description>Evaluate memory rules (R6).</description></item>
/// <item><description>Select highest-ranked keyword present (tie-break by script order).</description></item>
/// <item><description>Try decompositions in order; select/cycle reassemblies; process directives.</description></item>
/// <item><description>On <c>NEWKEY</c>, advance to next candidate; on <c>link</c>/<c>prelink</c>, jump accordingly.</description></item>
/// <item><description>If no reply: emit memory (per policy), else fall back to <c>none</c> lines.</description></item>
/// </list>
/// </para>
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by the RustyKane.com website.")]
public sealed class ElizaEngine
{
    private readonly DoctorScript _script;
    private readonly Tokenizer _tokenizer;
    private readonly SubstitutionService _subs;
    private readonly KeywordSelector _selector;
    private readonly PatternMatcher _matcher;
    private readonly ReassemblySelector _reasmSelector;
    private readonly MemoryManager _memory;

    private readonly Dictionary<string, Keyword> _byKey;
    private readonly StringComparer _keyCmp = StringComparer.OrdinalIgnoreCase;

    private int _turnIndex = 0;
    private int _noneCursor = 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="ElizaEngine"/> class.
    /// </summary>
    /// <param name="enableMemory">Whether to enable memory (R6) behavior.</param>
    /// <param name="sentenceCase">Whether to apply sentence casing to rendered replies.</param>
    /// <param name="ensureTerminalPunctuation">Whether to append '.' if no terminal punctuation is present.</param>
    public ElizaEngine(
        bool enableMemory = true,
        bool sentenceCase = true,
        bool ensureTerminalPunctuation = true)
    {
        DoctorScript script = ScriptLoader.Load("DOCTOR.json")
                              ?? throw new InvalidOperationException("Failed to load DOCTOR script.");

        _script = script;

        _tokenizer = new Tokenizer { NormalizeUnicode = true, KeepPunctuationTokens = false, LowercaseTokens = false };
        _subs = new SubstitutionService(script);
        _selector = new KeywordSelector(script, caseInsensitive: true);
        _matcher = new PatternMatcher(script, caseInsensitive: true);
        _reasmSelector = new ReassemblySelector(caseInsensitiveKeys: true);
        _memory = new MemoryManager(script, enabled: enableMemory);

        SentenceCase = sentenceCase;
        EnsureTerminalPunctuation = ensureTerminalPunctuation;

        _byKey = script.Keywords
            .GroupBy(k => k.Key ?? string.Empty, _keyCmp)
            .ToDictionary(g => g.Key, g => g.First(), _keyCmp);
    }

    /// <summary>
    /// Gets or sets a value indicating whether sentence casing is applied to final replies.
    /// </summary>
    public bool SentenceCase { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether terminal punctuation is ensured on final replies.
    /// </summary>
    public bool EnsureTerminalPunctuation { get; set; }

    /// <summary>
    /// Gets the initial greeting line from the script.
    /// </summary>
    public string Greeting => _script.Greeting;

    /// <summary>
    /// Generates a single ELIZA reply to the given raw user input.
    /// </summary>
    /// <param name="input">Raw user input for this turn.</param>
    /// <returns>The generated reply string.</returns>
    public string Reply(string? input)
    {
        _turnIndex++;

        // 1) Tokenize and preprocess
        List<string> tokens = _tokenizer.Tokenize(input ?? string.Empty);
        List<string> preTokens = _subs.ProcessInputTokens(tokens);

        // 2) Evaluate memory (R6) side-channel
        _memory.Evaluate(preTokens);

        // 3) Find candidate keywords present in input
        List<Keyword> candidates = _selector.FindCandidates(preTokens);

        // If no candidates, we will immediately try memory / NONE
        if (candidates.Count == 0)
        {
            if (TryEmitMemory(out string? memText))
            {
                return memText;
            }

            return EmitNone();
        }

        // 4) Attempt to produce a reply by walking candidates, handling directives
        List<string> workingTokens = preTokens;
        int candIndex = 0;
        int guardSteps = 0;
        const int MaxSteps = 32;  // safety against pathological loops
        const int MaxLinkDepth = 8;

        int linkDepth = 0;
        Keyword? current = candidates[candIndex];

        while (guardSteps++ < MaxSteps)
        {
            // Links-only keyword (R4)
            if ((current.Decompositions == null || current.Decompositions.Count == 0)
                && !string.IsNullOrWhiteSpace(current.Link))
            {
                if (!TryGetKeyword(current.Link!, out Keyword? target))
                {
                    break; // invalid link; bail to fallback
                }

                current = target;
                if (++linkDepth > MaxLinkDepth)
                {
                    break;
                }

                continue;
            }

            // Try decompositions in order
            bool matchedAnyDecomp = false;
            bool restart = false;

            if (current.Decompositions != null)
            {
                List<Decomposition> decomps = current.Decompositions;
                int decompCount = decomps.Count;

                for (int di = 0; di < decompCount; di++)
                {
                    if (current.Decompositions == null)
                    {
                        break;
                    }

                    Decomposition decomp = decomps[di];

                    if (_matcher.TryMatch(workingTokens, decomp, out List<string>? captures))
                    {
                        matchedAnyDecomp = true;

                        // Choose next reassembly for (keyword,decomp)
                        ReassemblyItem item = _reasmSelector.Select(current.Key, di, decomp.Reassemblies);

                        // Process item / directive
                        DirectiveOutcome outcome = DirectiveProcessor.Process(
                            item,
                            captures,
                            postMap: _subs.PostMap,
                            applyPost: true,
                            sentenceCase: SentenceCase,
                            ensureTerminalPunctuation: EnsureTerminalPunctuation);

                        switch (outcome.Action)
                        {
                            case DirectiveAction.EmitText:
                            {
                                return outcome.Text ?? EmitNone();
                            }

                            case DirectiveAction.NewKey:
                            {
                                if (!AdvanceCandidate(candidates, ref candIndex, out current))
                                {
                                    if (TryEmitMemory(out string? mem1))
                                    {
                                        return mem1;
                                    }

                                    return EmitNone();
                                }

                                // Reset working tokens to original preTokens when leaving link/jump path
                                workingTokens = preTokens;
                                restart = true;
                                continue;
                            }

                            case DirectiveAction.Link:
                            {
                                if (string.IsNullOrWhiteSpace(outcome.LinkTarget)
                                    || !TryGetKeyword(outcome.LinkTarget!, out Keyword? linkKw))
                                {
                                    if (TryEmitMemory(out string? mem2))
                                    {
                                        return mem2;
                                    }

                                    return EmitNone();
                                }

                                current = linkKw;

                                if (++linkDepth > MaxLinkDepth)
                                {
                                    if (TryEmitMemory(out string? mem3))
                                    {
                                        return mem3;
                                    }

                                    return EmitNone();
                                }

                                restart = true;
                                continue;
                            }

                            case DirectiveAction.Prelink:
                            {
                                if (string.IsNullOrWhiteSpace(outcome.LinkTarget) ||
                                    !TryGetKeyword(outcome.LinkTarget!, out Keyword? prelinkKw))
                                {
                                    if (TryEmitMemory(out string? mem4))
                                    {
                                        return mem4;
                                    }

                                    return EmitNone();
                                }

                                // Tokenize the transformed input; DO NOT apply pre-substitutions here.
                                List<string> tkns = _tokenizer.Tokenize(outcome.TransformedInput ?? string.Empty);

                                workingTokens = tkns;

                                current = prelinkKw;

                                if (++linkDepth > MaxLinkDepth)
                                {
                                    if (TryEmitMemory(out string? mem5))
                                    {
                                        return mem5;
                                    }

                                    return EmitNone();
                                }

                                restart = true;
                                continue;
                            }

                            default:
                            {
                                // Unknown/None directive—fall through to try next decomposition
                                break;
                            }
                        }

                        if (restart)
                        {
                            break;
                        }
                    }
                }
            }

            // If no decomposition matched for this keyword:
            // - If we arrived here via link/prelink, behave like NEWKEY (advance candidates).
            // - Otherwise, also advance to next candidate.
            if (!matchedAnyDecomp)
            {
                if (!AdvanceCandidate(candidates, ref candIndex, out current))
                {
                    if (TryEmitMemory(out string? mem6))
                    {
                        return mem6;
                    }

                    return EmitNone();
                }

                // Reset working tokens to original preTokens when leaving link path
                workingTokens = preTokens;
                continue;
            }

            // Safety net: if we matched but produced no directive text, advance candidate
            if (!AdvanceCandidate(candidates, ref candIndex, out current))
            {
                if (TryEmitMemory(out string? mem7))
                {
                    return mem7;
                }

                return EmitNone();
            }

            workingTokens = preTokens;
        }

        // Guard exceeded; fallback
        if (TryEmitMemory(out string? mem))
        {
            return mem;
        }

        return EmitNone();
    }

    /// <summary>
    /// Advances to the next keyword candidate; returns false if none left.
    /// </summary>
    private static bool AdvanceCandidate(List<Keyword> candidates, ref int index, out Keyword current)
    {
        index++;
        if (index >= candidates.Count)
        {
            current = null!;
            return false;
        }

        current = candidates[index];
        return true;
    }

    /// <summary>
    /// Emits a memory response (if available and allowed by the current policy).
    /// </summary>
    private bool TryEmitMemory(out string text)
    {
        text = string.Empty;

        if (_memory.TryDequeue(_turnIndex - 1, out string? queued) && !string.IsNullOrWhiteSpace(queued))
        {
            // IMPORTANT: Do NOT run post-substitutions on memory text.
            string rendered = TemplateRenderer.Render(
                queued!,
                captures: new List<string> { string.Empty },   // 1-based list with no captures
                postMap: null,                                  // <-- no post map
                applyPost: false,                               // <-- do not apply post
                sentenceCase: SentenceCase,
                ensureTerminalPunctuation: EnsureTerminalPunctuation);

            text = rendered;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Emits a NONE fallback reply, cycling through the script’s list.
    /// </summary>
    private string EmitNone()
    {
        if (_script.None == null || _script.None.Count == 0)
        {
            return string.Empty;
        }

        int idx = _noneCursor % _script.None.Count;
        _noneCursor++;

        // Render through the same pipeline for consistency.
        return TemplateRenderer.Render(
            _script.None[idx],
            captures: new List<string> { string.Empty },
            postMap: _subs.PostMap,
            applyPost: true,
            sentenceCase: SentenceCase,
            ensureTerminalPunctuation: EnsureTerminalPunctuation);
    }

    /// <summary>
    /// Resolves a keyword by its key (case-insensitive).
    /// </summary>
    private bool TryGetKeyword(string key, out Keyword kw) =>
        _byKey.TryGetValue(key, out kw!);
}
