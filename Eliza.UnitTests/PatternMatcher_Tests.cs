// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using FluentAssertions;
using Genova.Eliza;
using Genova.Eliza.Models;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class PatternMatcher_Tests
{
    private static DoctorScript CreateTestScript()
    {
        return new DoctorScript
        {
            Lexicon = new Dictionary<string, LexEntry>
            {
                { "mother", new LexEntry { Tags = ["FAMILY"] } },
                { "father", new LexEntry { Tags = ["FAMILY"] } },
                { "think", new LexEntry { Tags = ["BELIEF"] } },
                { "dog", new LexEntry { Tags = ["ANIMAL"] } }
            }
        };
    }

    [Fact]
    public void TryMatch_literal_token_success()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new LiteralToken { Value = "hello" } };
        var tokens = new List<string> { "hello" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "hello"]);
    }

    [Fact]
    public void TryMatch_literal_token_failure()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new LiteralToken { Value = "hello" } };
        var tokens = new List<string> { "hi" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeFalse();
        captures.Should().BeEmpty();
    }

    [Fact]
    public void TryMatch_wildcard_token_greedy()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new WildcardToken(), new LiteralToken { Value = "mother" } };
        var tokens = new List<string> { "my", "dear", "mother" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "my dear", "mother"]);
    }

    [Fact]
    public void TryMatch_wildcard_token_empty()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new WildcardToken(), new LiteralToken { Value = "hello" } };
        var tokens = new List<string> { "hello" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "", "hello"]);
    }

    [Fact]
    public void TryMatch_alts_token_success()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new AltsToken() { Alts = [ "hi", "hello", "hey" ] } };
        var tokens = new List<string> { "hey" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "hey"]);
    }

    [Fact]
    public void TryMatch_alts_token_failure()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new AltsToken() { Alts = ["hi", "hello", "hey"] } };
        var tokens = new List<string> { "greetings" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeFalse();
        captures.Should().BeEmpty();
    }

    [Fact]
    public void TryMatch_tag_token_success()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new TagToken { Tag = "FAMILY" } };
        var tokens = new List<string> { "mother" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "mother"]);
    }

    [Fact]
    public void TryMatch_tag_token_failure()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new TagToken { Tag = "FAMILY" } };
        var tokens = new List<string> { "dog" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeFalse();
        captures.Should().BeEmpty();
    }

    [Fact]
    public void TryMatch_complex_pattern_success()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken>
        {
            new WildcardToken(),
            new LiteralToken { Value = "think" },
            new TagToken { Tag = "FAMILY" }
        };
        var tokens = new List<string> { "i", "think", "father" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "i", "think", "father"]);
    }

    [Fact]
    public void TryMatch_decomposition_overload()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var decomposition = new Decomposition
        {
            Pattern = [new WildcardToken(), new LiteralToken { Value = "dog" }]
        };
        var tokens = new List<string> { "my", "dog" };

        matcher.TryMatch(tokens, decomposition, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "my", "dog"]);
    }

    [Fact]
    public void TryMatch_is_case_insensitive_by_default()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script);
        var pattern = new List<PatternToken> { new LiteralToken { Value = "hello" } };
        var tokens = new List<string> { "HELLO" };

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeTrue();
        captures.Should().BeEquivalentTo(["", "HELLO"]);
    }

    [Fact]
    public void TryMatch_can_be_case_sensitive()
    {
        DoctorScript script = CreateTestScript();
        PatternMatcher matcher = new (script, caseInsensitive: false);
        List<PatternToken> pattern = [new LiteralToken { Value = "hello" }];
        List<string> tokens = [ "HELLO" ];

        matcher.TryMatch(tokens, pattern, out List<string>? captures).Should().BeFalse();
        captures.Should().BeEmpty();
    }
}
