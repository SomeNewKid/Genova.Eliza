// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Genova.Common.Utilities;
using Genova.Eliza.Models;

namespace Genova.Eliza;

/// <summary>
/// Loads the ELIZA “DOCTOR” script from an embedded <c>DOCTOR.json</c> resource.
/// </summary>
internal static class ScriptLoader
{
    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Conflicting naming rules.")]
    private static readonly JsonSerializerOptions CachedOptions = CreateOptions();

    /// <summary>
    /// Loads and deserializes the <c>DOCTOR.json</c> script into a <see cref="DoctorScript"/> instance.
    /// <para>
    /// Returns <c>null</c> if the resource is missing/empty or if deserialization fails.
    /// </para>
    /// </summary>
    /// <param name="filename">The name of the script.</param>
    /// <returns>
    /// A populated <see cref="DoctorScript"/> instance on success; otherwise <c>null</c>.
    /// </returns>
    public static DoctorScript? Load(string filename)
    {
        string normalizedPath = $"Data/{filename}";

        string? content = null;
        Stream? stream = FileHelper.GetEmbeddedResourceStream(typeof(ScriptLoader), normalizedPath);
        if (stream != null)
        {
            using (StreamReader reader = new(stream))
            {
                content = reader.ReadToEnd();
            }
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        // try
        // {
        return JsonSerializer.Deserialize<DoctorScript>(content, CachedOptions);

        // }
        // catch (Exception)
        // {
        //     return null;
        // }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };
        options.Converters.Add(new PatternTokenConverter());
        options.Converters.Add(new ReassemblyItemConverter());
        return options;
    }
}
