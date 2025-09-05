// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Genova.Eliza;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class DoctorScript_Tests
{
    [Fact]
    public void DoctorScript_LoadedScript_Should_MeetStructuralInvariants()
    {
        // Arrange
        DoctorScript script = DoctorScript.Load();

        // Assert: Greeting exists
        script.Hello.Should().NotBeNullOrWhiteSpace("the script must have a greeting line");

        // Assert: Memory block is present and has a keyword
        script.Memory.Should().NotBeNull("the script must have a memory block");
        script.Memory.Keyword.Should().NotBeNullOrWhiteSpace("the memory block must have a keyword");

        // Assert: Memory block has exactly four rules
        script.Memory.Rules.Should().NotBeNull("the memory block must have rules");
        script.Memory.Rules.Count.Should().Be(4, "the memory block must have exactly four rules");

        // Assert: None list is not null
        script.None.Should().NotBeNull("the script must have a NONE fallback list");

        // Assert: At least one keyword is defined
        script.Keywords.Should().NotBeNull("the script must have keywords");
        script.Keywords.Count.Should().BeGreaterThan(0, "the script must have at least one keyword");

        // Assert: Each keyword's decompositions are well formed
        foreach (KeywordEntry kw in script.Keywords)
        {
            kw.Keyword.Should().NotBeNullOrWhiteSpace("each keyword must have a non-empty 'keyword'");
            kw.Decompositions.Should().NotBeNull("each keyword must have decompositions");

            foreach (Decomposition d in kw.Decompositions)
            {
                bool hasPattern = d.Pattern is { Count: > 0 };
                bool hasReasm = d.Reassembly is { Count: > 0 };
                bool hasLink = !string.IsNullOrWhiteSpace(d.Link);

                bool isLinkOnly = hasLink && !hasPattern;
                bool isPreLink = hasPattern && hasLink && !hasReasm;

                if (isLinkOnly || isPreLink)
                {
                    continue;
                }

                hasPattern.Should().BeTrue($"keyword '{kw.Keyword}' must have a decomposition with a non-empty pattern unless it's a link-only or pre+link rule");
                hasReasm.Should().BeTrue($"keyword '{kw.Keyword}' must have a decomposition with reassembly unless it's a link-only or pre+link rule");
            }
        }
    }
}
