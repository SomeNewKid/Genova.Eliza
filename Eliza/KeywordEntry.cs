using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Genova.Eliza;

internal sealed class KeywordEntry
{
    [JsonPropertyName("keyword")]
    public string Keyword { get; init; } = string.Empty;

    /// <summary>Optional synonym substitution: (= WORD) pattern at keyword level.</summary>
    [JsonPropertyName("substitution")]
    public string? Substitution { get; init; }

    /// <summary>Optional tags from DLIST (e.g., ["NOUN","FAMILY"]).</summary>
    [JsonPropertyName("dlist")]
    public List<string>? DList { get; init; }

    [JsonPropertyName("precedence")]
    public int? Precedence { get; init; }

    [JsonPropertyName("decompositions")]
    public List<Decomposition> Decompositions { get; init; } = new();
}
