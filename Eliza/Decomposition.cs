using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Genova.Eliza;

internal sealed class Decomposition
{
    /// <summary>Keyword link: (=WORD)</summary>
    [JsonPropertyName("link")]
    public string? Link { get; init; }

    /// <summary>Structured pattern tokens.</summary>
    [JsonPropertyName("pattern")]
    public List<PatternToken>? Pattern { get; init; }

    /// <summary>Reassembly lines (ordered).</summary>
    [JsonPropertyName("reassembly")]
    public List<string>? Reassembly { get; init; }

    /// <summary>Optional PRE directive text when present.</summary>
    [JsonPropertyName("pre")]
    public string? Pre { get; init; }
}
