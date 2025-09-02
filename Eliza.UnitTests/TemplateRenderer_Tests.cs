// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;

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


    [Fact]
    public void Render_replaces_simple_capture_placeholders()
    {
        string template = "You said: $1.";
        IReadOnlyList<string> captures = new List<string> { "", "I am sad" };
        string result = TemplateRenderer.Render(template, captures);
        result.Should().Be("You said: i am sad.");
    }

    [Fact]
    public void Render_handles_multiple_captures()
    {
        string template = "$1 and $2.";
        IReadOnlyList<string> captures = new List<string> { "", "I am sad", "I am tired" };
        string result = TemplateRenderer.Render(template, captures);
        result.Should().Be("i am sad and i am tired.");
    }

    [Fact]
    public void Render_ignores_out_of_range_capture_indices()
    {
        string template = "First: $1, Second: $2, Third: $3.";
        IReadOnlyList<string> captures = new List<string> { "", "foo" };
        string result = TemplateRenderer.Render(template, captures);
        result.Should().Be("First: foo, Second:, Third:.");
    }

    [Fact]
    public void Render_applies_post_map_to_captures_when_enabled()
    {
        string template = "You said $1.";
        IReadOnlyList<string> captures = new List<string> { "", "your dog is here" };
        IReadOnlyDictionary<string, string> postMap = new Dictionary<string, string>
        {
            { "your", "my" },
            { "dog", "cat" }
        };
        string result = TemplateRenderer.Render(template, captures, postMap, applyPost: true);
        result.Should().Be("You said my cat is here.");
    }

    [Fact]
    public void Render_does_not_apply_you_or_you_re_post_map_to_captures()
    {
        string template = "You say $1.";
        IReadOnlyList<string> captures = new List<string> { "", "you are happy" };
        IReadOnlyDictionary<string, string> postMap = new Dictionary<string, string>
        {
            { "you", "I" },
            { "you're", "I'm" },
            { "are", "am" }
        };
        string result = TemplateRenderer.Render(template, captures, postMap, applyPost: true);
        // "you" and "you're" are not flipped inside captures, but "are" is.
        result.Should().Be("You say you am happy.");
    }

    [Fact]
    public void Render_does_not_apply_post_map_when_disabled()
    {
        string template = "You said $1.";
        IReadOnlyList<string> captures = new List<string> { "", "your dog is here" };
        IReadOnlyDictionary<string, string> postMap = new Dictionary<string, string>
        {
            { "your", "my" },
            { "dog", "cat" }
        };
        string result = TemplateRenderer.Render(template, captures, postMap, applyPost: false);
        result.Should().Be("You said your dog is here.");
    }

    [Fact]
    public void Render_applies_sentence_case_when_enabled()
    {
        string template = "you said $1.";
        IReadOnlyList<string> captures = new List<string> { "", "i am sad" };
        string result = TemplateRenderer.Render(template, captures, sentenceCase: true);
        result.Should().Be("You said i am sad.");
    }

    [Fact]
    public void Render_appends_terminal_punctuation_when_missing()
    {
        string template = "You said $1";
        IReadOnlyList<string> captures = new List<string> { "", "I am sad" };
        string result = TemplateRenderer.Render(template, captures, ensureTerminalPunctuation: true);
        result.Should().Be("You said i am sad.");
    }

    [Fact]
    public void Render_does_not_append_terminal_punctuation_if_present()
    {
        string template = "You said $1!";
        IReadOnlyList<string> captures = new List<string> { "", "I am sad" };
        string result = TemplateRenderer.Render(template, captures, ensureTerminalPunctuation: true);
        result.Should().Be("You said i am sad!");
    }

    [Fact]
    public void Render_handles_empty_template_and_captures()
    {
        string template = "";
        IReadOnlyList<string> captures = new List<string> { "" };
        string result = TemplateRenderer.Render(template, captures);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Render_throws_on_null_template()
    {
        IReadOnlyList<string> captures = new List<string> { "" };
        Assert.Throws<System.ArgumentNullException>(() =>
            TemplateRenderer.Render(null!, captures));
    }

    [Fact]
    public void Render_throws_on_null_captures()
    {
        string template = "Hello $1";
        Assert.Throws<System.ArgumentNullException>(() =>
            TemplateRenderer.Render(template, null!));
    }
}
