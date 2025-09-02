// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using FluentAssertions;
using Genova.Eliza;
using Genova.Eliza.Models;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class ReassemblySelector_Tests
{
    private static List<ReassemblyItem> CreateTemplateItems(params string[] templates)
    {
        List<ReassemblyItem> items = new(templates.Length);
        foreach (string t in templates)
        {
            items.Add(new TemplateItem { Template = t });
        }
        return items;
    }

    [Fact]
    public void Select_cycles_through_template_items()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems(
            "Why do you ask?",
            "Does that question interest you?",
            "What is it you really want to know?"
        );

        string key = "WHAT";
        int decompIndex = 0;

        selector.Select(key, decompIndex, reassemblies).As<TemplateItem>().Template.Should().Be("Why do you ask?");
        selector.Select(key, decompIndex, reassemblies).As<TemplateItem>().Template.Should().Be("Does that question interest you?");
        selector.Select(key, decompIndex, reassemblies).As<TemplateItem>().Template.Should().Be("What is it you really want to know?");
        selector.Select(key, decompIndex, reassemblies).As<TemplateItem>().Template.Should().Be("Why do you ask?"); // wrap
    }

    [Fact]
    public void Select_can_handle_mixed_concrete_types()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies =
        [
            new TemplateItem { Template = "Hello." },
            new LinkDirective { Link = "WHAT" },
            new NewKeyDirective { NewKey = true },
            new PrelinkDirective { Prelink = new Prelink { Template = "I are $3", Link = "YOU" } }
        ];

        selector.Select("HELLO", 0, reassemblies).Should().BeOfType<TemplateItem>();
        selector.Select("HELLO", 0, reassemblies).Should().BeOfType<LinkDirective>();
        selector.Select("HELLO", 0, reassemblies).Should().BeOfType<NewKeyDirective>();
        selector.Select("HELLO", 0, reassemblies).Should().BeOfType<PrelinkDirective>();
        selector.Select("HELLO", 0, reassemblies).Should().BeOfType<TemplateItem>(); // wrap
    }

    [Fact]
    public void Select_is_independent_for_different_keys_and_decomp_indices()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> whatReassemblies = CreateTemplateItems("A", "B");
        IList<ReassemblyItem> whyReassemblies = CreateTemplateItems("X", "Y");

        selector.Select("WHAT", 0, whatReassemblies).As<TemplateItem>().Template.Should().Be("A");
        selector.Select("WHY", 0, whyReassemblies).As<TemplateItem>().Template.Should().Be("X");
        selector.Select("WHAT", 0, whatReassemblies).As<TemplateItem>().Template.Should().Be("B");
        selector.Select("WHY", 0, whyReassemblies).As<TemplateItem>().Template.Should().Be("Y");
        selector.Select("WHAT", 0, whatReassemblies).As<TemplateItem>().Template.Should().Be("A");
        selector.Select("WHY", 0, whyReassemblies).As<TemplateItem>().Template.Should().Be("X");
    }

    [Fact]
    public void Select_respects_case_insensitive_keys_by_default()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo", "bar");

        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
        selector.Select("what", 0, reassemblies).As<TemplateItem>().Template.Should().Be("bar");
        selector.Select("WhAt", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
    }

    [Fact]
    public void Select_respects_case_sensitive_keys_when_configured()
    {
        ReassemblySelector selector = new(caseInsensitiveKeys: false);
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo", "bar");

        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
        selector.Select("what", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo"); // new key, starts at 0
        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("bar");
        selector.Select("what", 0, reassemblies).As<TemplateItem>().Template.Should().Be("bar");
    }

    [Fact]
    public void Select_throws_on_null_arguments_or_empty_reassemblies()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo");

        Assert.Throws<System.ArgumentNullException>(() => selector.Select(null!, 0, reassemblies));
        Assert.Throws<System.ArgumentNullException>(() => selector.Select("WHAT", 0, null!));
        Assert.Throws<System.ArgumentException>(() => selector.Select("WHAT", 0, []));
    }

    [Fact]
    public void TrySelect_returns_false_on_null_or_empty_args()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo");

        selector.TrySelect(null!, 0, reassemblies, out ReassemblyItem? _).Should().BeFalse();
        selector.TrySelect("WHAT", 0, null!, out _).Should().BeFalse();
        selector.TrySelect("WHAT", 0, [], out _).Should().BeFalse();
    }

    [Fact]
    public void TrySelect_returns_true_and_selects_item()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo", "bar");

        selector.TrySelect("WHAT", 0, reassemblies, out ReassemblyItem? item).Should().BeTrue();
        item!.As<TemplateItem>().Template.Should().Be("foo");
        selector.TrySelect("WHAT", 0, reassemblies, out item).Should().BeTrue();
        item!.As<TemplateItem>().Template.Should().Be("bar");
    }

    [Fact]
    public void Reset_resets_cursor_for_key_and_decomp()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo", "bar");

        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("bar");
        selector.Reset("WHAT", 0);
        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
    }

    [Fact]
    public void Clear_resets_all_cursors()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo", "bar");

        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("bar");
        selector.Clear();
        selector.Select("WHAT", 0, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
    }

    [Fact]
    public void Select_handles_negative_decomposition_index_as_zero()
    {
        ReassemblySelector selector = new();
        IList<ReassemblyItem> reassemblies = CreateTemplateItems("foo", "bar");

        selector.Select("WHAT", -1, reassemblies).As<TemplateItem>().Template.Should().Be("foo");
        selector.Select("WHAT", -99, reassemblies).As<TemplateItem>().Template.Should().Be("bar");
    }
}
