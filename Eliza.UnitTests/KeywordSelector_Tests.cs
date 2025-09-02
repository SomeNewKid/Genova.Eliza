// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using FluentAssertions;
using Genova.Eliza;
using Genova.Eliza.Models;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class KeywordSelector_Tests
{
    private static DoctorScript CreateTestScript()
    {
        // Minimal script with a few keywords for testing
        return new DoctorScript
        {
            Keywords =
            [
                new Keyword { Key = "HELLO" },
                new Keyword { Key = "WHAT", Rank = 10 },
                new Keyword { Key = "MAYBE", Substitution = "PERHAPS", Rank = 5 },
                new Keyword { Key = "NAME", Rank = 15 },
                new Keyword { Key = "COMPUTER", Rank = 50 },
                new Keyword { Key = "MY", Substitution = "YOUR", Rank = 2 },
            ]
        };
    }

    [Fact]
    public void FindCandidates_returns_keywords_triggered_by_tokens()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script);

        List<string> tokens = ["hello", "what", "computer"];
        List<Keyword> candidates = selector.FindCandidates(tokens);

        candidates.Should().Contain(k => k.Key == "HELLO");
        candidates.Should().Contain(k => k.Key == "WHAT");
        candidates.Should().Contain(k => k.Key == "COMPUTER");
        candidates.Should().NotContain(k => k.Key == "NAME");
        candidates.Should().NotContain(k => k.Key == "MY");
        candidates.Should().NotContain(k => k.Key == "MAYBE");
    }

    [Fact]
    public void FindCandidates_triggers_on_substitution_terms()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script);

        List<string> tokens = ["perhaps", "your"];
        List<Keyword> candidates = selector.FindCandidates(tokens);

        candidates.Should().Contain(k => k.Key == "MAYBE");
        candidates.Should().Contain(k => k.Key == "MY");
    }

    [Fact]
    public void FindCandidates_sorts_by_rank_then_script_order()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script);

        List<string> tokens = ["what", "name", "computer"];
        List<Keyword> candidates = selector.FindCandidates(tokens);

        // Highest rank first: COMPUTER (50), then NAME (15), then WHAT (10)
        candidates[0].Key.Should().Be("COMPUTER");
        candidates[1].Key.Should().Be("NAME");
        candidates[2].Key.Should().Be("WHAT");
    }

    [Fact]
    public void FindBest_returns_highest_ranked_keyword()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script);

        List<string> tokens = ["what", "computer", "name"];
        Keyword? best = selector.FindBest(tokens);

        best.Should().NotBeNull();
        best!.Key.Should().Be("COMPUTER"); // Highest rank
    }

    [Fact]
    public void FindBest_returns_null_if_no_keywords_match()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script);

        List<string> tokens = ["banana", "apple"];
        Keyword? best = selector.FindBest(tokens);

        best.Should().BeNull();
    }

    [Fact]
    public void FindCandidates_is_case_insensitive_by_default()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script);

        List<string> tokens = ["HELLO", "wHaT", "CoMpUtEr"];
        List<Keyword> candidates = selector.FindCandidates(tokens);

        candidates.Should().Contain(k => k.Key == "HELLO");
        candidates.Should().Contain(k => k.Key == "WHAT");
        candidates.Should().Contain(k => k.Key == "COMPUTER");
    }

    [Fact]
    public void FindCandidates_can_be_case_sensitive()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script, caseInsensitive: false);

        List<string> tokens = ["HELLO", "WHAT", "COMPUTER"];
        List<Keyword> candidates = selector.FindCandidates(tokens);

        candidates.Should().Contain(k => k.Key == "HELLO");
        candidates.Should().Contain(k => k.Key == "WHAT");
        candidates.Should().Contain(k => k.Key == "COMPUTER");
    }

    [Fact]
    public void FindCandidates_throws_on_null_tokens()
    {
        DoctorScript script = CreateTestScript();
        KeywordSelector selector = new (script);

        Assert.Throws<System.ArgumentNullException>(() => selector.FindCandidates(null!));
    }
}
