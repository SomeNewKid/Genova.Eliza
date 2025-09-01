// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Interprets reassembly directives (e.g., NEWKEY, link, prelink) and produces
/// actionable outcomes for the ELIZA engine.
/// <para>
/// This processor centralizes the logic for handling directive-type reassembly items
/// and, for convenience, can also render plain template items into final text.
/// </para>
/// </summary>
internal sealed class DirectiveProcessor
{
    /// <summary>
    /// Processes a reassembly item and returns a <see cref="DirectiveOutcome"/> that
    /// indicates whether to emit text, advance to a new keyword, follow a link, or perform a prelink.
    /// <para>
    /// For <see cref="TemplateItem"/> inputs, this method renders the final text using the
    /// TemplateRenderer and options. For directive items (<see cref="NewKeyDirective"/>,
    /// <see cref="LinkDirective"/>, <see cref="PrelinkDirective"/>), it returns the appropriate action.
    /// </para>
    /// </summary>
    /// <param name="item">The reassembly item to process.</param>
    /// <param name="captures">
    /// The 1-based capture list corresponding to <c>$1</c>, <c>$2</c>, … from the matched decomposition.
    /// Index 0 is unused.
    /// </param>
    /// <param name="postMap">
    /// Optional post-substitution map (e.g., pronoun flips) to apply when rendering <see cref="TemplateItem"/>.
    /// Ignored for directives and for the prelink’s intermediate template.
    /// </param>
    /// <param name="applyPost">Whether to apply <paramref name="postMap"/> to rendered template items.</param>
    /// <param name="sentenceCase">Whether to apply a simple sentence-casing pass to rendered template items.</param>
    /// <param name="ensureTerminalPunctuation">
    /// Whether to ensure rendered template items end with terminal punctuation if none is present.
    /// </param>
    /// <returns>
    /// A <see cref="DirectiveOutcome"/> describing the action the engine should take.
    /// </returns>
    public static DirectiveOutcome Process(
        ReassemblyItem item,
        IReadOnlyList<string> captures,
        IReadOnlyDictionary<string, string>? postMap = null,
        bool applyPost = true,
        bool sentenceCase = false,
        bool ensureTerminalPunctuation = false)
    {
        ArgumentNullException.ThrowIfNull(item);

        switch (item)
        {
            case TemplateItem t:
            {
                string text = TemplateRenderer.Render(
                    t.Template,
                    captures,
                    postMap,
                    applyPost,
                    sentenceCase,
                    ensureTerminalPunctuation);

                return DirectiveOutcome.Emit(text);
            }

            case NewKeyDirective:
            {
                return DirectiveOutcome.NewKey();
            }

            case LinkDirective link:
            {
                return DirectiveOutcome.Link(link.Link);
            }

            case PrelinkDirective pre:
            {
                // Render the intermediate text without post-substitutions or casing — it should
                // be treated like fresh input for the target keyword.
                string transformed = TemplateRenderer.Render(
                    pre.Prelink.Template,
                    captures,
                    postMap: null,
                    applyPost: false,
                    sentenceCase: false,
                    ensureTerminalPunctuation: false);

                return DirectiveOutcome.Prelink(transformed, pre.Prelink.Link);
            }

            default:
            {
                // Unknown directive type — treat as no-op; engine can fall back to NONE/memory.
                return DirectiveOutcome.None();
            }
        }
    }
}
