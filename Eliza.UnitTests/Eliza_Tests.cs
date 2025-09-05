// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

namespace Genova.Eliza.UnitTests;

public class Eliza_Tests
{
    [Fact]
    public void Outputs_are_deterministic()
    {
        Eliza e1 = new ();
        Eliza e2 = new ();

        string[] inputs = ["I feel sad.", "Yes", "I am angry."];

        string[] out1 = inputs.Select(i => e1.Reply(i)).ToArray();
        string[] out2 = inputs.Select(i => e2.Reply(i)).ToArray();

        Assert.Equal(out1, out2);
    }
}
