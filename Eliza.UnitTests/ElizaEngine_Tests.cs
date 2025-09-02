// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza.UnitTests;

public class ElizaEngine_Tests
{
    [Fact]
    public void Outputs_are_deterministic()
    {
        ElizaEngine e1 = new ();
        ElizaEngine e2 = new ();

        string[] inputs = ["I feel sad.", "Yes", "I am angry."];

        string[] out1 = inputs.Select(i => e1.Reply(i)).ToArray();
        string[] out2 = inputs.Select(i => e2.Reply(i)).ToArray();

        Assert.Equal(out1, out2);
    }

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

    [Fact]
    public void Memory_primary_pronouns_remain_user_perspective()
    {
        ElizaEngine engine = new();

        // Turn 1: enqueue memory via (0 YOUR 0) pattern.
        _ = engine.Reply("My family is annoying me.");

        // Turn 2: trigger NONE so memory emits.
        string reply = engine.Reply("Okay.");

        Assert.Contains("your family is annoying you", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("annoying i", reply, StringComparison.OrdinalIgnoreCase);
    }


    [Fact]
    public void Covers_R1_to_R6_in_order()
    {
        ElizaEngine engine = new();

        // Ordered (input, expectedSubstring) pairs.
        // Each assertion triggers exactly one rule-family we care about.
        var steps = new (string input, string expect)[]
        {
            // R1: I/FEEL decomposition
            ("I feel sad.", "Tell me more about such feelings."),

            // R2: simple substitution (recollect -> remember) hitting REMEMBER
            ("I recollect the time we met.", "You say you remember the time we met?"),

            // R4: link-only keyword (MACHINES -> COMPUTER, rank 50)
            ("Machines worry me.", "Do computers worry you?"),

            // R5: prelink (YOU'RE = I'M -> PRE (I ARE 3) (=YOU))
            ("You're helpful.", "What makes you think I am helpful?"),

            // R3: DLIST tag (/FAMILY) through MY rule
            ("Let me tell you about my mother.", "Tell me more about your family."),

            // R6: memory emission after a fallback (NONE)
            // The first queued memory line should be "Let's discuss further why your $3."
            ("Okay.", "Let's discuss further why your mother.")
        };

        foreach ((string input, string expect) in steps)
        {
            string reply = engine.Reply(input);
            Assert.Contains(expect, reply, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Prelink_YOURE_hits_YOU_I_ARE_pattern()
    {
        ElizaEngine engine = new();

        string reply = engine.Reply("You're helpful.");

        Assert.Contains("What makes you think I am helpful", reply, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Can you elaborate", reply, StringComparison.OrdinalIgnoreCase);
    }
}
