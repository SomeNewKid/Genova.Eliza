// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using FluentAssertions;
using Genova.Eliza;
using Genova.Eliza.Enums;
using Genova.Eliza.Models;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class DirectiveProcessor_Tests
{
    [Fact]
    public void Process_TemplateItem_renders_text_with_captures_and_options()
    {
        TemplateItem item = new () { Template = "Hello $1!" };
        List<string> captures = ["", "world"];
        Dictionary<string, string> postMap = new() { { "world", "universe" } };

        DirectiveOutcome outcome = DirectiveProcessor.Process(item, captures, postMap, applyPost: true, sentenceCase: true, ensureTerminalPunctuation: true);

        outcome.Action.Should().Be(DirectiveAction.EmitText);
        outcome.Text.Should().Be("Hello universe!");
        outcome.LinkTarget.Should().BeNull();
        outcome.TransformedInput.Should().BeNull();
    }

    [Fact]
    public void Process_TemplateItem_renders_text_without_post_or_casing()
    {
        TemplateItem item = new () { Template = "Hello $1" };
        List<string> captures = ["", "WORLD"];

        DirectiveOutcome outcome = DirectiveProcessor.Process(item, captures, postMap: null, applyPost: false, sentenceCase: false, ensureTerminalPunctuation: false);

        outcome.Action.Should().Be(DirectiveAction.EmitText);
        outcome.Text.Should().Be("Hello world");
    }

    [Fact]
    public void Process_NewKeyDirective_returns_NewKey_action()
    {
        NewKeyDirective item = new ();
        List<string> captures = [""];

        DirectiveOutcome outcome = DirectiveProcessor.Process(item, captures);

        outcome.Action.Should().Be(DirectiveAction.NewKey);
        outcome.Text.Should().BeNull();
        outcome.LinkTarget.Should().BeNull();
        outcome.TransformedInput.Should().BeNull();
    }

    [Fact]
    public void Process_LinkDirective_returns_Link_action_and_target()
    {
        LinkDirective item = new () { Link = "WHAT" };
        List<string> captures = [""];

        DirectiveOutcome outcome = DirectiveProcessor.Process(item, captures);

        outcome.Action.Should().Be(DirectiveAction.Link);
        outcome.LinkTarget.Should().Be("WHAT");
        outcome.Text.Should().BeNull();
        outcome.TransformedInput.Should().BeNull();
    }

    [Fact]
    public void Process_PrelinkDirective_returns_Prelink_action_and_transformed_input()
    {
        PrelinkDirective item = new ()
        {
            Prelink = new Prelink
            {
                Template = "I am $1",
                Link = "YOU"
            }
        };
        List<string> captures = ["", "happy"];
        Dictionary<string, string> postMap = new() { { "happy", "sad" } };

        DirectiveOutcome outcome = DirectiveProcessor.Process(item, captures, postMap, applyPost: true);

        outcome.Action.Should().Be(DirectiveAction.Prelink);
        outcome.LinkTarget.Should().Be("YOU");
        outcome.TransformedInput.Should().Be("I am happy");
        outcome.Text.Should().BeNull();
    }

    [Fact]
    public void Process_unknown_type_returns_None_action()
    {
        DummyReassemblyItem item = new ();
        List<string> captures = [""];

        DirectiveOutcome outcome = DirectiveProcessor.Process(item, captures);

        outcome.Action.Should().Be(DirectiveAction.None);
        outcome.Text.Should().BeNull();
        outcome.LinkTarget.Should().BeNull();
        outcome.TransformedInput.Should().BeNull();
    }

    [Fact]
    public void Process_throws_on_null_item()
    {
        List<string> captures = [""];
        Assert.Throws<System.ArgumentNullException>(() => DirectiveProcessor.Process(null!, captures));
    }

    // Dummy type for unknown directive coverage
    private sealed record DummyReassemblyItem : ReassemblyItem { }
}
