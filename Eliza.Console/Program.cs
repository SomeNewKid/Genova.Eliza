// This file is part of the Genova project licensed under the GNU General Public License v3.0.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace Genova.Eliza.Console;

internal class Program
{
    /// <summary>
    /// The main entry point for the Genova Eliza.Console application.
    /// Runs an instance of the ELIZA chatbot in the console.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application.</param>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Convention of the Main method.")]
    static void Main(string[] args)
    {
        Eliza engine = new ();

        // Initial greeting.
        string greeting = engine.Greeting;
        if (string.IsNullOrEmpty(greeting))
        {
            greeting = "Hello. How can I help you today?";
        }
        System.Console.WriteLine($"ELIZA> {greeting}");

        // Conversation loop.
        while (true)
        {
            System.Console.Write("YOU> ");
            string? input = System.Console.ReadLine();

            // EOF (Ctrl+Z / Ctrl+D) or explicit exit commands end the session.
            if (input is null) break;
            string trimmed = input.Trim();
            if (trimmed.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("bye", StringComparison.OrdinalIgnoreCase))
            {
                break;
            }

            string reply = engine.Reply(input);
            System.Console.WriteLine($"ELIZA> {reply}");
        }

        System.Console.WriteLine("ELIZA> Goodbye.");
    }
}
