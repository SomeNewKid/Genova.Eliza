// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using FluentAssertions;
using Genova.Eliza;
using Genova.Eliza.Enums;
using Genova.Eliza.Models;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class MemoryManager_Tests
{
    private static DoctorScript CreateScriptWithMemory(params MemoryRule[] rules)
    {
        return new DoctorScript
        {
            Memory = [.. rules]
        };
    }

    [Fact]
    public void Constructor_sets_enabled_and_initial_policy()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script, enabled: false);

        mgr.Enabled.Should().BeFalse();
        mgr.EmissionPolicy.Should().Be(MemoryEmissionPolicy.FallbackOnly);
        mgr.InterleaveEvery.Should().Be(0);
        mgr.PendingCount.Should().Be(0);
        mgr.HasPending.Should().BeFalse();
    }

    [Fact]
    public void Clear_empties_queue()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Enqueue("foo");
        mgr.Enqueue("bar");
        mgr.PendingCount.Should().Be(2);
        mgr.Clear();
        mgr.PendingCount.Should().Be(0);
        mgr.HasPending.Should().BeFalse();
    }

    [Fact]
    public void Enqueue_adds_to_queue_when_enabled_and_nonempty()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Enqueue("foo");
        mgr.Enqueue("  bar  ");
        mgr.PendingCount.Should().Be(2);
        mgr.HasPending.Should().BeTrue();
    }

    [Fact]
    public void Enqueue_ignores_null_or_whitespace()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Enqueue(null);
        mgr.Enqueue("");
        mgr.Enqueue("   ");
        mgr.PendingCount.Should().Be(0);
    }

    [Fact]
    public void Enqueue_ignores_when_disabled()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script, enabled: false);
        mgr.Enqueue("foo");
        mgr.PendingCount.Should().Be(0);
    }

    [Fact]
    public void Evaluate_enqueues_matching_memory_rule()
    {
        MemoryRule rule = new ()
        {
            Keyword = "MY",
            Pattern = [new LiteralToken() { Value = "my" }, new WildcardToken()],
            Template = "Tell me more about your $2."
        };
        DoctorScript script = CreateScriptWithMemory(rule);
        MemoryManager mgr = new (script);

        mgr.Evaluate(new List<string> { "my", "dog" });
        mgr.PendingCount.Should().Be(1);
        mgr.HasPending.Should().BeTrue();
        mgr.TryDequeue(null, out string? response).Should().BeTrue();
        response.Should().Be("Tell me more about your dog.");
    }

    [Fact]
    public void Evaluate_does_not_enqueue_when_disabled()
    {
        MemoryRule rule = new ()
        {
            Keyword = "MY",
            Pattern = [new LiteralToken() { Value = "my" }, new WildcardToken()],
            Template = "Tell me more about your $2."
        };
        DoctorScript script = CreateScriptWithMemory(rule);
        MemoryManager mgr = new (script, enabled: false);

        mgr.Evaluate(new List<string> { "my", "cat" });
        mgr.PendingCount.Should().Be(0);
    }

    [Fact]
    public void Evaluate_does_not_enqueue_when_no_rules_or_tokens()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Evaluate(new List<string>());
        mgr.PendingCount.Should().Be(0);
        mgr.Evaluate(null!);
        mgr.PendingCount.Should().Be(0);
    }

    [Fact]
    public void TryDequeue_returns_false_when_disabled_or_empty()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script, enabled: false);
        mgr.TryDequeue(null, out string? _).Should().BeFalse();
        mgr.Enabled = true;
        mgr.TryDequeue(null, out _).Should().BeFalse();
    }

    [Fact]
    public void TryDequeue_returns_false_when_policy_is_Off()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Enqueue("foo");
        mgr.EmissionPolicy = MemoryEmissionPolicy.Off;
        mgr.TryDequeue(null, out string? _).Should().BeFalse();
    }

    [Fact]
    public void TryDequeue_returns_false_when_policy_is_InterleaveEveryN_and_not_due()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Enqueue("foo");
        mgr.EmissionPolicy = MemoryEmissionPolicy.InterleaveEveryN;
        mgr.InterleaveEvery = 2;
        mgr.TryDequeue(0, out string? _).Should().BeFalse();
        mgr.TryDequeue(1, out string? response).Should().BeTrue();
        response.Should().Be("foo");
    }

    [Fact]
    public void TryDequeue_returns_true_when_policy_is_Opportunistic()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Enqueue("foo");
        mgr.EmissionPolicy = MemoryEmissionPolicy.Opportunistic;
        mgr.TryDequeue(0, out string? response).Should().BeTrue();
        response.Should().Be("foo");
    }

    [Fact]
    public void TryDequeue_returns_true_when_policy_is_FallbackOnly()
    {
        DoctorScript script = CreateScriptWithMemory();
        MemoryManager mgr = new (script);
        mgr.Enqueue("foo");
        mgr.EmissionPolicy = MemoryEmissionPolicy.FallbackOnly;
        mgr.TryDequeue(null, out string? response).Should().BeTrue();
        response.Should().Be("foo");
    }
}
