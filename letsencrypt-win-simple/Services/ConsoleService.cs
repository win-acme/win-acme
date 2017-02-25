using System;
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
            Console.WriteLine(message + " (y/n)");
            var response = Console.ReadKey(true);
            switch (response.Key)
            {
                case ConsoleKey.Y:
                    return true;
                case ConsoleKey.N:
                    return false;
            }
            return false;
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
    }
}
