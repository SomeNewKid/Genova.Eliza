// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using Genova.Common.Attributes;

namespace Genova.Eliza;

/// <summary>
/// Orchestrates a full ELIZA turn using a loaded <see cref="DoctorScript"/>.
/// </summary>
[CodeQuality(Public = true, Justification = "Intended for use by the RustyKane.com website.")]
public sealed class Eliza
{
    private readonly DoctorScript _script;
    private readonly ElizaEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="Eliza"/> class.
    /// </summary>
    public Eliza()
    {
        _script = DoctorScript.Load();
        _engine = new ElizaEngine(_script);
    }

    /// <summary>
    /// Gets the initial greeting line from the script.
    /// </summary>
    public string Greeting
    {
        get
        {
            return _script.Hello;
        }
    }

    /// <summary>
    /// Generates a single ELIZA reply to the given raw user input.
    /// </summary>
    /// <param name="input">Raw user input for this turn.</param>
    /// <returns>The generated reply string.</returns>
    public string Reply(string? input)
    {
        return _engine.Reply(input ?? "Hello");
    }
}
