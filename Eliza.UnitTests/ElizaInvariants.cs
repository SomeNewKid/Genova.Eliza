// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Genova.Eliza.UnitTests;

internal static partial class ElizaInvariants
{
    /// <summary>
    /// Ensures no lowercase single-letter "i" pronoun slips into ELIZA's reply.
    /// Uppercase "I" and forms like "I'm", "I've", "I am" are permitted (and expected)
    /// in several templates per DOCTOR.json (e.g., NAME, ARE, AM, YOU, XFREMD, NONE, YES).
    /// </summary>
    public static void AssertNoLowercaseI(string reply) =>
        Assert.DoesNotMatch(LowercaseIPronoun(), reply);

    /// <summary>
    /// Basic presentation: reply should start with a capital letter.
    /// </summary>
    public static void AssertStartsWithCapital(string reply) =>
        Assert.True(!string.IsNullOrWhiteSpace(reply) && char.IsUpper(reply.TrimStart()[0]),
            $"Reply should start with a capital: \"{reply}\"");

    /// <summary>
    /// Perspective sanity: if the user spoke in first person ("I", "I'm", "I am", "my"),
    /// the bot should not mirror that back as *lowercase* "i". Uppercase "I" is allowed
    /// because many script templates intentionally use the therapist's "I".
    /// </summary>
    public static void AssertPerspectiveFlip(string input, string reply)
    {
        if (PerspectiveFlip().IsMatch(input))
        {
            // Only enforce the lowercase 'i' ban (do NOT forbid uppercase "I")
            AssertNoLowercaseI(reply);
        }
    }

    // Fail if the reply contains a lowercase single-letter pronoun "i"
    // as a standalone token or with an apostrophe (e.g., "i", "i'm", "i've").
    // Word-boundaries via \W are used so "in", "aid" etc. won't match.
    [GeneratedRegex(@"(?<=^|\W)i(?=\W|$)", RegexOptions.Compiled)]
    private static partial Regex LowercaseIPronoun();

    [GeneratedRegex(@"(?i)\b(i|i'm|i am|my)\b", RegexOptions.None, "en-AU")]
    private static partial Regex PerspectiveFlip();
}
