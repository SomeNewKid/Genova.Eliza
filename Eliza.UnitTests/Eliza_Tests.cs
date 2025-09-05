// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

namespace Genova.Eliza.UnitTests;

public class Eliza_Tests
{
    [Fact]
    public void Outputs_are_deterministic()
    {
        Eliza e1 = new ();
        Eliza e2 = new ();

        string[] inputs = ["I feel sad.", "Yes", "I am angry."];

        string[] out1 = inputs.Select(i => e1.Reply(i)).ToArray();
        string[] out2 = inputs.Select(i => e2.Reply(i)).ToArray();

        Assert.Equal(out1, out2);
    }

    [Fact]
    public void Typicial_conversation_test()
    {
        Eliza eliza = new();

        eliza.Reply("Men are all alike.")
            .Should().Be("IN WHAT WAY");

        eliza.Reply("They're always bugging us about something or other.")
            .Should().Be("CAN YOU THINK OF A SPECIFIC EXAMPLE");

        eliza.Reply("Well, my boyfriend made me come here.")
            .Should().Be("YOUR BOYFRIEND MADE YOU COME HERE");

        eliza.Reply("He says I'm depressed much of the time.")
            .Should().Be("I AM SORRY TO HEAR YOU ARE DEPRESSED");

        eliza.Reply("It's true. I am unhappy.")
            .Should().Be("DO YOU THINK COMING HERE WILL HELP YOU NOT TO BE UNHAPPY");

        eliza.Reply("I need some help, that much seems certain.")
            .Should().Be("WHAT WOULD IT MEAN TO YOU IF YOU GOT SOME HELP");

        eliza.Reply("Perhaps I could learn to get along with my mother.")
            .Should().Be("TELL ME MORE ABOUT YOUR FAMILY");

        eliza.Reply("My mother takes care of me.")
            .Should().Be("WHO ELSE IN YOUR FAMILY TAKES CARE OF YOU");

        eliza.Reply("My father.")
            .Should().Be("YOUR FATHER");

        eliza.Reply("You are like my father in some ways.")
            .Should().Be("WHAT RESEMBLANCE DO YOU SEE");

        eliza.Reply("You are not very aggressive but I think you don't want me to notice that.")
            .Should().Be("WHAT MAKES YOU THINK I AM NOT VERY AGGRESSIVE");

        eliza.Reply("You don't argue with me.")
            .Should().Be("WHY DO YOU THINK I DON'T ARGUE WITH YOU");

        eliza.Reply("You are afraid of me.")
            .Should().Be("DOES IT PLEASE YOU TO BELIEVE I AM AFRAID OF YOU");

        eliza.Reply("My father is afraid of everybody.")
            .Should().Be("WHAT ELSE COMES TO MIND WHEN YOU THINK OF YOUR FATHER");

        eliza.Reply("Bullies.")
            .Should().Be("DOES THAT HAVE ANYTHING TO DO WITH THE FACT THAT YOUR BOYFRIEND MADE YOU COME HERE");
    }

}
