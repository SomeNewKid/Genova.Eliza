// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using FluentAssertions;
using Genova.Eliza.Models;

namespace Genova.Eliza.UnitTests;

public class ScriptLoader_Tests
{
    [Fact]
    public void Doctor_script_can_be_loaded()
    {
        DoctorScript? script = ScriptLoader.Load("DOCTOR.json");
        script.Should().NotBeNull();
        script.Greeting.Should().Be("How do you do. Please tell me your problem.");
    }
}
