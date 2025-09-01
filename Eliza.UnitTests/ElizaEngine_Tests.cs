// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza.UnitTests;

public class ElizaEngine_Tests
{
    [Fact]
    public void Dream_template_pronoun_must_not_change()
    {
        // Repro for: “What does that dream suggest to you?” becoming “… to i?”
        ElizaEngine engine = new ();
        string input = "I had a dream.";

        string reply = engine.Reply(input);

        Assert.Contains("to you?", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("to i?", reply, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Elaboration_template_pronoun_must_not_change()
    {
        // Repro for: “Can you elaborate on that?” becoming “Can i elaborate on that?”
        ElizaEngine engine = new();
        string input = "I am angry.";

        string reply = engine.Reply(input);

        Assert.Contains("you", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("I", reply, StringComparison.OrdinalIgnoreCase);
    }
}
