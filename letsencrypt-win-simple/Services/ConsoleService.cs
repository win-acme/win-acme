using System;
using System.Collections.Generic;
using LetsEncrypt.ACME.Simple.Configuration;

namespace LetsEncrypt.ACME.Simple.Services
{
    public class ConsoleService
    {
        public string ReadCommandFromConsole()
        {
            return Console.ReadLine().ToLowerInvariant();
        }

        public bool PromptYesNo(string message)
        {
            ConsoleKey response;
            do
            {
                Console.Write(message + " (y/n)");
                response = Console.ReadKey(false).Key;
                if (response != ConsoleKey.Enter)
                    Console.WriteLine();

            } while (response != ConsoleKey.Y && response != ConsoleKey.N);

            return response == ConsoleKey.Y;
        }

        public void PrintMenuForPlugins()
        {
            // Check for a plugin specified in the options
            // Only print the menus if there's no plugin specified
            // Otherwise: you actually have no choice, the specified plugin will run
            if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
                return;

            foreach (var plugin in Target.Plugins.Values)
            {
                if (string.IsNullOrEmpty(App.Options.ManualHost))
                {
                    plugin.PrintMenu();
                }
                else if (plugin.Name == "Manual")
                {
                    plugin.PrintMenu();
                }
            }
        }

        public void PrintMenu(List<Target> targets)
        {
            if (string.IsNullOrEmpty(App.Options.ManualHost) && string.IsNullOrWhiteSpace(App.Options.Plugin))
            {
                Console.WriteLine(" A: Get certificates for all hosts");
                Console.WriteLine(" Q: Quit");
                Console.Write("Which host do you want to get a certificate for: ");
                var command = App.ConsoleService.ReadCommandFromConsole();
                switch (command)
                {
                    case "a":
                        App.CertificateService.GetCertificatesForAllHosts(targets);
                        break;
                    case "q":
                        return;
                    default:
                        Target.ProcessDefaultCommand(targets, command);
                        break;
                }
            }
            else if (!string.IsNullOrWhiteSpace(App.Options.Plugin))
            {
                // If there's a plugin in the options, only do ProcessDefaultCommand for the selected plugin
                // Plugins that can run automatically should allow for an empty string as menu response to work
                Target.ProcessDefaultCommand(targets, string.Empty);
            }
        }
    }
}
