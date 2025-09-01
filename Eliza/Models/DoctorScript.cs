// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Text.Json.Serialization;

namespace Genova.Eliza.Models;

/// <summary>
/// Root object representing the ELIZA “DOCTOR” script.
/// <para>
/// This class mirrors the structure of <c>DOCTOR.json</c>:
/// greeting text, substitution tables, DLIST-style lexicon,
/// ordered keyword rules, memory queue rules, and NONE fallbacks.
/// </para>
/// </summary>
internal sealed class DoctorScript
{
    /// <summary>
    /// Gets or sets the initial line ELIZA prints at startup.
    /// Example: <c>"How do you do. Please tell me your problem."</c>.
    /// </summary>
    [JsonPropertyName("greeting")]
    public string Greeting { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the collections of normalization and viewpoint-flip substitutions:
    /// <list type="bullet">
    /// <item>
    /// <description><c>simple</c>: lexical normalization (e.g., <c>"dont" → "don't"</c>).</description>
    /// </item>
    /// <item>
    /// <description><c>pre</c>: applied to user input before matching (pronoun flips, etc.).</description>
    /// </item>
    /// <item>
    /// <description><c>post</c>: applied to generated replies before output.</description>
    /// </item>
    /// </list>
    /// Keys are case-insensitive in practice if you normalize tokens.
    /// </summary>
    [JsonPropertyName("substitutions")]
    public Substitutions Substitutions { get; set; } = new();

    /// <summary>
    /// Gets or sets the DLIST-style lexicon mapping a surface word to optional
    /// canonical substitution and a set of semantic/grammatical tags.
    /// <para>
    /// Example entry: <c>"mom": { "substitution": "mother", "tags": ["FAMILY"] }</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("lexicon")]
    public Dictionary<string, LexEntry> Lexicon { get; set; } = [];

    /// <summary>
    /// Gets or sets the ordered list of keyword rules (R1–R5 and link-only rules).
    /// <para>
    /// Rank determines priority; order acts as a tie-breaker when ranks are equal.
    /// Within each keyword, decomposition order is match-first, and
    /// reassemblies are cycled across successive matches.
    /// </para>
    /// </summary>
    [JsonPropertyName("keywords")]
    public List<Keyword> Keywords { get; set; } = [];

    /// <summary>
    /// Gets or sets the memory rules (R6). When a memory decomposition matches,
    /// the associated template is queued for later use by the engine.
    /// </summary>
    [JsonPropertyName("memory")]
    public List<MemoryRule> Memory { get; set; } = [];

    /// <summary>
    /// Gets or sets the fallback responses used when no keyword matches (the NONE rule).
    /// </summary>
    [JsonPropertyName("none")]
    public List<string> None { get; set; } = [];
}
