using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using System.Text;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Interfaces;

namespace LetsEncrypt.ACME.Simple.Core.Services
{
    public class ConsoleService : IConsoleService
    {
        protected IOptions Options;
        public ConsoleService(IOptions options)
        {
            Options = options;
        }

        public string ReadCommandFromConsole()
        {
            var readLine = Console.ReadLine();
            return readLine != null ? readLine.ToLowerInvariant() : string.Empty;
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

        public void PromptEnter(string message = "Press enter to continue.")
        {
            if (!string.IsNullOrWhiteSpace(Options.Plugin))
                return;

            Console.WriteLine(message);
            Console.ReadLine();
        }

        public void WriteLine(string message)
        {
            Console.WriteLine(message);
        }

        public void Write(string message)
        {
            Console.Write(message);
        }

        public void WriteError(string message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(
                "\n******************************************************************************");

            Console.WriteLine(message);

            Console.WriteLine(
                "\n******************************************************************************");
            Console.ResetColor();
        }

        public string ReadLine()
        {
            var readLine = Console.ReadLine();
            return readLine != null ? readLine.Trim() : string.Empty;
        }

        public void PrintMenuForPlugins()
        {
            foreach (var plugin in Options.Plugins.Values)
                if (string.IsNullOrEmpty(Options.ManualHost))
                    plugin.PrintMenu();
                else if (plugin.Name == "Manual")
                    plugin.PrintMenu();
        }

        public void WriteQuitCommandInformation()
        {
            Console.WriteLine(" Q: Quit");
            Console.Write("Press enter to continue to next page ");
        }

        public string[] GetSanNames()
        {
            Console.Write("Enter all Alternative Names seperated by a comma ");
            Console.SetIn(new System.IO.StreamReader(Console.OpenStandardInput(8192)));
            var sanInput = ReadLine();
            return sanInput.Split(',');
        }

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        public string ReadPassword()
        {
            var password = new StringBuilder();
            try
            {
                var info = Console.ReadKey(true);
                while (info.Key != ConsoleKey.Enter)
                {
                    if (info.Key != ConsoleKey.Backspace)
                    {
                        Console.Write("*");
                        password.Append(info.KeyChar);
                    }
                    else if (info.Key == ConsoleKey.Backspace)
                    {
                        if (password.Length > 0)
                        {
                            // remove one character from the list of password characters
                            password.Remove(password.Length - 1, 1);
                            // get the location of the cursor
                            var pos = Console.CursorLeft;
                            // move the cursor to the left by one character
                            Console.SetCursorPosition(pos - 1, Console.CursorTop);
                            // replace it with space
                            Console.Write(" ");
                            // move the cursor to the left by one character again
                            Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        }
                    }
                    info = Console.ReadKey(true);
                }
                // add a new line because user pressed enter at the end of their password
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Log.Error("Error Reading Password: {@ex}", ex);
            }

            return password.ToString();
        }
        public void WriteBindings(List<Target> targets)
        {
            if (targets.Count == 0 && string.IsNullOrEmpty(Options.ManualHost))
            {
                Log.Error("No targets found.");
            }
            else
            {
                var hostsPerPage = Options.HostsPerPage;
                if (targets.Count > hostsPerPage)
                    WriteBindingsFromTargetsPaged(targets, hostsPerPage, 1);
                else
                    WriteBindingsFromTargetsPaged(targets, targets.Count, 1);
                WriteLine("");
            }
        }

        private void WriteBindingsFromTargetsPaged(List<Target> targets, int pageSize, int fromNumber)
        {
            do
            {
                var toNumber = fromNumber + pageSize;
                if (toNumber <= targets.Count)
                    fromNumber = WriteBindingsFromTargets(targets, toNumber, fromNumber);
                else
                    fromNumber = WriteBindingsFromTargets(targets, targets.Count + 1, fromNumber);

                if (fromNumber >= targets.Count)
                    continue;

                WriteQuitCommandInformation();
                var command = ReadCommandFromConsole();

                if (command == "q")
                    throw new Exception("Requested to quit application");

            } while (fromNumber < targets.Count);
        }

        private int WriteBindingsFromTargets(List<Target> targets, int toNumber, int fromNumber)
        {
            for (var i = fromNumber; i < toNumber; i++)
            {
                if (!Options.San)
                    WriteLine($" {i}: {targets[i - 1]}");
                else
                    WriteLine($" {targets[i - 1].SiteId}: SAN - {targets[i - 1]}");
                fromNumber++;
            }

            return fromNumber;
        }

        public void HandleMenuResponseForPlugins(List<Target> targets, string command)
        {
            // Only run the plugin specified in the config
            if (!string.IsNullOrWhiteSpace(Options.Plugin))
            {
                var plugin = Options.Plugins.Values.FirstOrDefault(x => string.Equals(x.Name, Options.Plugin,
                    StringComparison.InvariantCultureIgnoreCase));
                if (plugin != null)
                {
                    plugin.HandleMenuResponse(command, targets);
                }
                else
                {
                    Log.Information("Plugin '{AppOptionsPlugin}' could not be found.", Options.Plugin);
                    PromptEnter("Press enter to exit");
                }
            }
            else
            {
                foreach (var plugin in Options.Plugins.Values)
                    plugin.HandleMenuResponse(command, targets);
            }
        }
    }
}
