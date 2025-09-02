// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using FluentAssertions;
using Genova.Eliza;
using Genova.Eliza.Models;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class SubstitutionService_Tests
{
    private static DoctorScript CreateTestScript()
    {
        return new DoctorScript
        {
            Substitutions = new Substitutions
            {
                Simple = new Dictionary<string, string>
                {
                    { "dont", "don't" },
                    { "cant", "can't" },
                    { "recollect", "remember" }
                },
                Pre = new Dictionary<string, string>
                {
                    { "i", "you" },
                    { "my", "your" },
                    { "am", "are" }
                },
                Post = new Dictionary<string, string>
                {
                    { "you", "I" },
                    { "your", "my" },
                    { "are", "am" }
                }
            },
            Lexicon = new Dictionary<string, LexEntry>
            {
                { "mom", new LexEntry { Substitution = "mother", Tags = ["FAMILY"] } },
                { "dad", new LexEntry { Substitution = "father", Tags = ["FAMILY"] } }
            }
        };
    }

    [Fact]
    public void ProcessInputTokens_applies_simple_lexicon_and_pre_subs()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        List<string> input = ["dont", "mom", "i", "am", "happy"];
        List<string> output = service.ProcessInputTokens(input);

        output.Should().BeEquivalentTo(["don't", "mother", "you", "are", "happy"]);
    }

    [Fact]
    public void ProcessInputTokens_is_case_insensitive()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        List<string> input = ["DONT", "MoM", "I", "AM", "HAPPY"];
        List<string> output = service.ProcessInputTokens(input);

        output.Should().BeEquivalentTo(["don't", "mother", "you", "are", "HAPPY"]);
    }

    [Fact]
    public void ProcessInputTokens_returns_empty_for_empty_input()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        List<string> input = [];
        List<string> output = service.ProcessInputTokens(input);

        output.Should().BeEmpty();
    }

    [Fact]
    public void ApplyPostToText_applies_post_subs_and_preserves_casing()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        string input = "You are happy. YOUR mom is here.";
        string output = service.ApplyPostToText(input);

        output.Should().Be("I am happy. MY mom is here.");
    }

    [Fact]
    public void ApplyPostToText_handles_mixed_casing()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        string input = "YOU ARE happy. Your dad is here.";
        string output = service.ApplyPostToText(input);

        output.Should().Be("I AM happy. My dad is here.");
    }

    [Fact]
    public void ApplyPostToText_returns_original_if_no_post_subs()
    {
        DoctorScript script = CreateTestScript();
        script.Substitutions.Post.Clear();
        SubstitutionService service = new (script);

        string input = "You are happy.";
        string output = service.ApplyPostToText(input);

        output.Should().Be("You are happy.");
    }

    [Fact]
    public void ApplyPostToTokens_applies_post_subs_to_tokens()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        List<string> input = ["you", "are", "your", "mom"];
        List<string> output = service.ApplyPostToTokens(input);

        output.Should().BeEquivalentTo(["I", "am", "my", "mom"]);
    }

    [Fact]
    public void SimpleMap_PreMap_PostMap_are_case_insensitive()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        service.SimpleMap.ContainsKey("DONT").Should().BeTrue();
        service.PreMap.ContainsKey("I").Should().BeTrue();
        service.PostMap.ContainsKey("YOU").Should().BeTrue();
    }

    [Fact]
    public void Throws_on_null_arguments()
    {
        DoctorScript script = CreateTestScript();
        SubstitutionService service = new (script);

        Assert.Throws<System.ArgumentNullException>(() => new SubstitutionService(null!));
        Assert.Throws<System.ArgumentNullException>(() => service.ProcessInputTokens(null!));
        Assert.Throws<System.ArgumentNullException>(() => service.ApplyPostToTokens(null!));
    }
}
