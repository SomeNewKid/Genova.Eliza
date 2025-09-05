using System.Text.Json.Serialization;

namespace Genova.Eliza;

internal sealed class MemoryBlock
{
    [JsonPropertyName("keyword")]
    public string Keyword { get; init; } = string.Empty;

    [JsonPropertyName("rules")]
    public List<MemoryRule> Rules { get; init; } = new();
}
