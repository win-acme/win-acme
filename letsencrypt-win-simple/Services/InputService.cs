using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LetsEncrypt.ACME.Simple.Services
{
    class InputService
    {
        private Options _options;

        public InputService(Options options)
        {
            _options = options;
        }

        private void Validate(string what)
        {
            if (_options.Renew && !_options.Test)
            {
                throw new Exception($"User input '{what}' should not be needed in --renew mode.");
            }
        }

        public void Wait()
        {
            if (!_options.Renew)
            {
                Console.Write(" Press enter to continue... ");
                while (true)
                {
                    var response = Console.ReadKey(true);
                    switch (response.Key)
                    {
                        case ConsoleKey.Enter:
                            Console.WriteLine();
                            Console.WriteLine();
                            return;
                    }
                }
            }
        }

        public string RequestString(string what)
        {
            Validate(what);
            var answer = string.Empty;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" {what}: ");
            Console.ResetColor();
            answer = Console.ReadLine();
            Console.WriteLine();
            return answer.Trim();
        }

        public bool PromptYesNo(string message)
        {
            Validate(message);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.Write($" {message} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"(y/n): ");
            Console.ResetColor();
            while (true)
            {
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.Y:
                        Console.WriteLine("- yes");
                        Console.WriteLine();
                        return true;
                    case ConsoleKey.N:
                        Console.WriteLine("- no");
                        Console.WriteLine();
                        return false;
                }
            }
        }

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        public string ReadPassword(string what)
        {
            Validate(what);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" {what}: ");
            Console.ResetColor();
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
                        if (password.Length > 0)
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
                Program.Log.Error("Error Reading Password: {@ex}", ex);
            }

            return password.ToString();
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="targets"></param>
        public void WriteTargets(List<Target> targets)
        {
            if (targets.Count == 0)
            {
                Program.Log.Warning("No targets found.");
            }
            else
            {
                var hostsPerPage = Program.Settings.HostsPerPage();
                var currentPlugin = "";
                var currentIndex = 0;
                var currentPage = 0;

                while (currentIndex <= targets.Count - 1)
                {
                    // Paging
                    if (currentIndex > 0)
                    {
                        Wait();
                        currentPage += 1;
                    }
                    var page = targets.Skip(currentPage * hostsPerPage).Take(hostsPerPage);
                    foreach (var target in page)
                    {
                        // Seperate target from different plugins
                        if (!string.Equals(currentPlugin, target.PluginName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            currentPlugin = target.PluginName;
                            Console.WriteLine();
                        }
                        Console.WriteLine($" {currentIndex + 1}: {targets[currentIndex]}");
                        currentIndex++;
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
