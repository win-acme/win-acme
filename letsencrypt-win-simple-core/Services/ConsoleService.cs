using System;
using System.Collections.Generic;
using Serilog;
using System.Text;
using LetsEncrypt.ACME.Simple.Core.Configuration;

namespace LetsEncrypt.ACME.Simple.Core.Services
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

        public void PromptEnter(string message = "Press enter to continue.")
        {
            if (string.IsNullOrWhiteSpace(App.Options.Plugin))
            {
                Console.WriteLine(message);
                Console.ReadLine();
            }
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
            return Console.ReadLine().Trim();
        }

        public void PrintMenuForPlugins()
        {
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
            if (string.IsNullOrEmpty(App.Options.ManualHost))
            {
                Console.WriteLine(" A: Get certificates for all hosts");
                Console.WriteLine(" Q: Quit");
                Console.Write("Choose from one of the menu options above: ");
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
            var sanInput = App.ConsoleService.ReadLine();
            return sanInput.Split(',');
        }
        
        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        public string ReadPassword()
        {
            var password = new StringBuilder();
            try
            {
                ConsoleKeyInfo info = Console.ReadKey(true);
                while (info.Key != ConsoleKey.Enter)
                {
                    if (info.Key != ConsoleKey.Backspace)
                    {
                        Console.Write("*");
                        password.Append(info.KeyChar);
                    }
                    else if (info.Key == ConsoleKey.Backspace)
                    {
                        if (password != null)
                        {
                            // remove one character from the list of password characters
                            password.Remove(password.Length - 1, 1);
                            // get the location of the cursor
                            int pos = Console.CursorLeft;
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
    }
}
