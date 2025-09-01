// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Eliza.Enums;
using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Manages ELIZA memory (R6) behavior for a conversation session.
/// <para>
/// The manager evaluates <see cref="DoctorScript.Memory"/> rules against the user’s
/// (preprocessed) input, enqueues matching templates for later use, and exposes
/// methods to emit queued memory responses according to a chosen policy.
/// </para>
/// </summary>
internal sealed class MemoryManager
{
    private readonly DoctorScript _script;
    private readonly Queue<string> _queue = new();
    private readonly PatternMatcher _matcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryManager"/> class.
    /// </summary>
    /// <param name="script">The loaded <see cref="DoctorScript"/> that supplies memory rules.</param>
    /// <param name="enabled">Whether memory behavior is enabled.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="script"/> is null.</exception>
    public MemoryManager(DoctorScript script, bool enabled = true)
    {
        _script = script ?? throw new ArgumentNullException(nameof(script));
        Enabled = enabled;
        _matcher = new PatternMatcher(script, caseInsensitive: true);
    }

    /// <summary>
    /// Gets or sets a value indicating whether memory handling is enabled.
    /// When disabled, no rules are evaluated and no responses are emitted.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the emission policy controlling when queued memory
    /// responses are surfaced to the user.
    /// </summary>
    public MemoryEmissionPolicy EmissionPolicy { get; set; } = MemoryEmissionPolicy.FallbackOnly;

    /// <summary>
    /// Gets or sets the interleave cadence (in turns) used when
    /// <see cref="EmissionPolicy"/> is <see cref="MemoryEmissionPolicy.InterleaveEveryN"/>.
    /// A value of <c>0</c> or less disables interleaving.
    /// </summary>
    public int InterleaveEvery { get; set; } = 0;

    /// <summary>
    /// Gets the number of queued memory responses pending emission.
    /// </summary>
    public int PendingCount => _queue.Count;

    /// <summary>
    /// Gets a value indicating whether there is at least one queued memory response.
    /// </summary>
    public bool HasPending => _queue.Count > 0;

    /// <summary>
    /// Clears all queued memory responses.
    /// </summary>
    public void Clear() => _queue.Clear();

    /// <summary>
    /// Evaluates the script’s memory rules against the given (preprocessed) input tokens,
    /// and enqueues any templates whose patterns match (classic ELIZA R6 behavior).
    /// </summary>
    /// <param name="tokens">The preprocessed input tokens (after simple and pre substitutions).</param>
    public void Evaluate(IReadOnlyList<string> tokens)
    {
        if (!Enabled || _script.Memory.Count == 0 || tokens is null || tokens.Count == 0)
        {
            return;
        }

        foreach (MemoryRule rule in _script.Memory)
        {
            if (_matcher.TryMatch(tokens, rule.Pattern, out List<string>? captures))
            {
                // Render the memory template with captures; no post/casing here.
                string rendered = TemplateRenderer.Render(
                    rule.Template,
                    captures,
                    postMap: null,
                    applyPost: false,
                    sentenceCase: false,
                    ensureTerminalPunctuation: false);

                if (!string.IsNullOrWhiteSpace(rendered))
                {
                    Enqueue(rendered.Trim());
                }
            }
        }
    }

    /// <summary>
    /// Enqueues a memory response template explicitly.
    /// </summary>
    /// <param name="template">The text to enqueue (ignored if null/whitespace).</param>
    public void Enqueue(string? template)
    {
        if (!Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(template))
        {
            return;
        }

        _queue.Enqueue(template);
    }

    /// <summary>
    /// Attempts to dequeue the next memory response, honoring the current
    /// <see cref="EmissionPolicy"/>. This method does not apply post-processing;
    /// callers should run post-substitutions and casing as needed.
    /// </summary>
    /// <param name="turnIndex">
    /// The zero-based conversation turn index (used for interleaving decisions).
    /// Supply <c>null</c> if not applicable.
    /// </param>
    /// <param name="response">
    /// When this method returns, contains the dequeued response if available; otherwise <c>null</c>.
    /// </param>
    /// <returns><c>true</c> if a response was dequeued; otherwise <c>false</c>.</returns>
    public bool TryDequeue(int? turnIndex, out string? response)
    {
        response = null;
        if (!Enabled || _queue.Count == 0)
        {
            return false;
        }

        // Basic gating by policy (stubbed logic — adjust to taste).
        switch (EmissionPolicy)
        {
            case MemoryEmissionPolicy.Off:
            {
                return false;
            }

            case MemoryEmissionPolicy.FallbackOnly:
            {
                // Caller should only call this when no other reply was produced.
                break;
            }

            case MemoryEmissionPolicy.InterleaveEveryN:
            {
                if (InterleaveEvery <= 0 || turnIndex is null)
                {
                    return false;
                }

                if ((turnIndex.Value + 1) % InterleaveEvery != 0)
                {
                    return false;
                }

                break;
            }

            case MemoryEmissionPolicy.Opportunistic:
            {
                // Always allow emission when available.
                break;
            }
        }

        response = _queue.Dequeue();
        return true;
    }
}
