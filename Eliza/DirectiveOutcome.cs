// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Eliza.Enums;
using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Result object returned by <see cref="DirectiveProcessor.Process(ReassemblyItem, IReadOnlyList{string}, IReadOnlyDictionary{string, string}?, bool, bool, bool)"/>.
/// </summary>
internal sealed class DirectiveOutcome
{
    private DirectiveOutcome(DirectiveAction action) => Action = action;

    /// <summary>
    /// Gets the action that the engine should take (emit text, NEWKEY, link, or prelink).
    /// </summary>
    public DirectiveAction Action { get; }

    /// <summary>
    /// Gets the text to emit when <see cref="Action"/> is <see cref="DirectiveAction.EmitText"/>.
    /// Otherwise <c>null</c>.
    /// </summary>
    public string? Text { get; private set; }

    /// <summary>
    /// Gets the target keyword name when <see cref="Action"/> is <see cref="DirectiveAction.Link"/>
    /// or <see cref="DirectiveAction.Prelink"/>. Otherwise <c>null</c>.
    /// </summary>
    public string? LinkTarget { get; private set; }

    /// <summary>
    /// Gets the intermediate transformed input when <see cref="Action"/> is
    /// <see cref="DirectiveAction.Prelink"/>. Otherwise <c>null</c>.
    /// </summary>
    public string? TransformedInput { get; private set; }

    /// <summary>
    /// Creates an outcome representing no action.
    /// </summary>
    /// <returns>A DirectiveOutcome.</returns>
    public static DirectiveOutcome None() => new(DirectiveAction.None);

    /// <summary>
    /// Creates an outcome that emits the specified text.
    /// </summary>
    /// <param name="text">The text to emit.</param>
    /// <returns>A DirectiveOutcome.</returns>
    public static DirectiveOutcome Emit(string text) =>
        new(DirectiveAction.EmitText) { Text = text };

    /// <summary>
    /// Creates an outcome that instructs the engine to abandon the current keyword.
    /// </summary>
    /// <returns>A DirectiveOutcome.</returns>
    public static DirectiveOutcome NewKey() => new(DirectiveAction.NewKey);

    /// <summary>
    /// Creates an outcome that instructs the engine to jump to a target keyword.
    /// </summary>
    /// <param name="target">The link target keyword name.</param>
    /// <returns>A DirectiveOutcome.</returns>
    public static DirectiveOutcome Link(string target) =>
        new(DirectiveAction.Link) { LinkTarget = target };

    /// <summary>
    /// Creates an outcome that applies an intermediate transformation before linking.
    /// </summary>
    /// <param name="transformedInput">The transformed input text to use for the next step.</param>
    /// <param name="target">The link target keyword name.</param>
    /// <returns>A DirectiveOutcome.</returns>
    public static DirectiveOutcome Prelink(string transformedInput, string target) =>
        new(DirectiveAction.Prelink)
        {
            TransformedInput = transformedInput,
            LinkTarget = target,
        };
}
