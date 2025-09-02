// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using FluentAssertions;
using Genova.Eliza;
using Xunit;

namespace Genova.Eliza.UnitTests;

public class Tokenizer_Tests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public void Tokenize_returns_empty_list_for_null_or_whitespace(string? input)
    {
        Tokenizer tokenizer = new ();
        List<string> tokens = tokenizer.Tokenize(input);
        tokens.Should().BeEmpty();
    }

    [Fact]
    public void Tokenize_basic_word_and_number_extraction()
    {
        Tokenizer tokenizer = new ();
        List<string> tokens = tokenizer.Tokenize("Hello world 123 45.67");
        tokens.Should().BeEquivalentTo(["Hello", "world", "123", "45.67"]);
    }

    [Fact]
    public void Tokenize_preserves_apostrophes_within_words()
    {
        Tokenizer tokenizer = new ();
        List<string> tokens = tokenizer.Tokenize("you're don't it's");
        tokens.Should().BeEquivalentTo(["you're", "don't", "it's"]);
    }

    [Fact]
    public void Tokenize_excludes_punctuation_by_default()
    {
        Tokenizer tokenizer = new ();
        List<string> tokens = tokenizer.Tokenize("Hello, world!");
        tokens.Should().BeEquivalentTo(["Hello", "world"]);
    }

    [Fact]
    public void Tokenize_includes_punctuation_when_enabled()
    {
        Tokenizer tokenizer = new () { KeepPunctuationTokens = true };
        List<string> tokens = tokenizer.Tokenize("Hello, world!");
        tokens.Should().BeEquivalentTo(["Hello", ",", "world", "!"]);
    }

    [Fact]
    public void Tokenize_converts_tokens_to_lowercase_when_enabled()
    {
        Tokenizer tokenizer = new () { LowercaseTokens = true };
        List<string> tokens = tokenizer.Tokenize("Hello WORLD");
        tokens.Should().BeEquivalentTo(["hello", "world"]);
    }

    [Fact]
    public void Tokenize_handles_unicode_normalization()
    {
        Tokenizer tokenizer = new ();
        string input = "“Hello” — world…\u00A0It’s fine.";
        List<string> tokens = tokenizer.Tokenize(input);
        tokens.Should().BeEquivalentTo(["Hello", "world", "It's", "fine"]);
    }

    [Fact]
    public void Tokenize_can_disable_unicode_normalization()
    {
        Tokenizer tokenizer = new () { NormalizeUnicode = false, KeepPunctuationTokens = true };
        string input = "“Hello” — world… It’s fine.";
        List<string> tokens = tokenizer.Tokenize(input);
        tokens.Should().Contain("It");
        tokens.Should().Contain("s");
        tokens.Should().NotContain("It's");
        tokens.Should().NotContain("It’s");
    }

    [Theory]
    [InlineData(".", true)]
    [InlineData("?", true)]
    [InlineData("!", true)]
    [InlineData(",", true)]
    [InlineData(";", true)]
    [InlineData(":", true)]
    [InlineData("a", false)]
    [InlineData("word", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("..", false)]
    public void IsPunctuationToken_returns_expected(string token, bool expected)
    {
        bool result = Tokenizer.IsPunctuationToken(token);
        result.Should().Be(expected);
    }

    [Fact]
    public void NormalizeText_replaces_typographic_characters_and_collapses_whitespace()
    {
        string input = "“Hello”   world…  It’s   fine.\u00A0";
        string normalized = Tokenizer.NormalizeText(input);
        normalized.Should().Be("\"Hello\" world... It's fine.");
    }

    [Fact]
    public void CollapseWhitespace_trims_and_collapses_spaces()
    {
        MethodInfo? method = typeof(Tokenizer).GetMethod("CollapseWhitespace", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        string input = "   a   b   c   ";
        string? result = (string)method.Invoke(null, [input])!;
        result.Should().NotBeNull();
        result.Should().Be("a b c");
    }
}
