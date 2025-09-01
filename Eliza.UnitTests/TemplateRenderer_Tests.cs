// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza.UnitTests;

public class TemplateRenderer_Tests
{
    [Fact]
    public void PostSubstitutions_applied_to_captures_only()
    {
        // Simulate post map with the problematic “you”→“i”
        Dictionary<string, string> postMap = new (StringComparer.OrdinalIgnoreCase)
        {
            ["you"] = "i",
            ["your"] = "my",
            ["yours"] = "mine",
            ["yourself"] = "myself"
        };

        // Template uses a fixed “You” that must NOT be altered by post–subs.
        string template = "You say $1?";
        // Capture came from user input after pre-processing; keep as-is for this check.
        List<string> captures = ["", "you feel sad"]; // 1-based: $1 = "you feel sad"

        // After the fix, only the capture is considered for post–subs (and here we can assert it’s unchanged
        // or, if you choose to post-sub captures, assert the expected transformed value).
        string result = TemplateRenderer.Render(
            template, captures, postMap, applyPost: true, sentenceCase: true, ensureTerminalPunctuation: false);

        // The template word “You” must remain “You”, i.e., NOT turned into “I”.
        Assert.Equal("You say you feel sad?", result);
    }
}
