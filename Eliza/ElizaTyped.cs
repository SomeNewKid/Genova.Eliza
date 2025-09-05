using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Genova.Common.Utilities;

namespace Genova.Eliza;

internal sealed class ElizaTyped
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules.")]
    private static readonly JsonSerializerOptions CachedOptions = CreateOptions();

    [JsonPropertyName("hello")]
    public string Hello { get; init; } = string.Empty;

    [JsonPropertyName("none")]
    public List<string> None { get; init; } = new();

    [JsonPropertyName("memory")]
    public MemoryBlock Memory { get; init; } = new();

    [JsonPropertyName("keywords")]
    public List<KeywordEntry> Keywords { get; init; } = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new PatternTokenListConverter() }
    };

    public static ElizaTyped Load(string filename)
    {
        string normalizedPath = $"Data/{filename}";

        string? content = null;
        Stream? stream = FileHelper.GetEmbeddedResourceStream(typeof(ElizaTyped), normalizedPath);
        if (stream != null)
        {
            using (StreamReader reader = new(stream))
            {
                content = reader.ReadToEnd();
            }
        }

        if (string.IsNullOrEmpty(content))
        {
            throw new FileNotFoundException("JSON file not found", filename);
        }

        var data = JsonSerializer.Deserialize<ElizaTyped>(content, Options);
        if (data is null) throw new InvalidDataException("Failed to deserialize DOCTOR.json.");
        return data;
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Hello))
            throw new InvalidDataException("Missing 'hello'.");
        if (Memory is null || string.IsNullOrWhiteSpace(Memory.Keyword))
            throw new InvalidDataException("Missing 'memory.keyword'.");
        if (Memory.Rules is null || Memory.Rules.Count != 4)
            throw new InvalidDataException($"'memory.rules' must have exactly 4 (found {Memory.Rules?.Count ?? 0}).");
        if (None is null) throw new InvalidDataException("'none' must not be null.");
        if (Keywords is null || Keywords.Count == 0)
            throw new InvalidDataException("No 'keywords' found.");

        foreach (var kw in Keywords)
        {
            if (string.IsNullOrWhiteSpace(kw.Keyword))
                throw new InvalidDataException("A keyword has an empty 'keyword'.");
            if (kw.Decompositions is null)
                throw new InvalidDataException($"Keyword '{kw.Keyword}' has null 'decompositions'.");

            foreach (var d in kw.Decompositions)
            {
                bool hasPattern = d.Pattern is { Count: > 0 };
                bool hasReasm = d.Reassembly is { Count: > 0 };
                bool hasLink = !string.IsNullOrWhiteSpace(d.Link);

                bool isLinkOnly = hasLink && !hasPattern;          // (=WORD)
                bool isPreLink = hasPattern && hasLink && !hasReasm; // pattern + (optional) pre + link

                if (isLinkOnly || isPreLink)
                    continue;

                if (!hasPattern)
                    throw new InvalidDataException($"Keyword '{kw.Keyword}' has a decomposition with empty 'pattern'.");

                if (!hasReasm)
                    throw new InvalidDataException($"Keyword '{kw.Keyword}' has a decomposition missing 'reassembly'.");
            }
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        return options;
    }
}
