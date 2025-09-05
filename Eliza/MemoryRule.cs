
using System.Text.Json.Serialization;

namespace Genova.Eliza;

internal sealed class MemoryRule
{
    [JsonPropertyName("pattern")]
    public List<PatternToken> Pattern { get; init; } = new();

    [JsonPropertyName("reassembly")]
    public string Reassembly { get; init; } = string.Empty;
}
