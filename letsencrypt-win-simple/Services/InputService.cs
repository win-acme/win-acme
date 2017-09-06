using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LetsEncrypt.ACME.Simple.Services
{
    class InputService
    {
        private Options _options;
        public bool LogMessage { get; set; }

        public InputService(Options options)
        {
            _options = options;
        }

        private void Validate(string what)
        {
            if (_options.Renew)
            {
                throw new Exception($"User input '{what}' should not be needed in --renew mode.");
            }
        }

        protected void CreateSpace()
        {
            if (LogMessage)
            {
                LogMessage = false;
                Console.WriteLine();
            }
        }

        public void Wait()
        {
            if (!_options.Renew)
            {
                CreateSpace();
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

        public string RequestString(string[] what)
        {
            if (what != null)
            {
                CreateSpace();
                Console.ForegroundColor = ConsoleColor.Green;
                for (var i = 0; i < what.Length - 1; i++)
                {              
                    Console.WriteLine($" {what[i]}");
                }
                Console.ResetColor();
                return RequestString(what[what.Length - 1]);
            }
            return string.Empty;
        }

        public string RequestString(string what)
        {
            Validate(what);
            var answer = string.Empty;
            CreateSpace();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" {what}: ");
            Console.ResetColor();

            // Copied from http://stackoverflow.com/a/16638000
            int bufferSize = 16384;
            Stream inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));

            answer = Console.ReadLine();
            Console.WriteLine();
            return answer.Trim();
        }

        public bool PromptYesNo(string message)
        {
            Validate(message);
            CreateSpace();
            Console.ForegroundColor = ConsoleColor.Green;
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
            CreateSpace();
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

                while (currentIndex < targets.Count - 1)
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

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="targets"></param>
        public T ChooseFromList<T>(string what, IEnumerable<T> options, Func<T, Choice<T>> creator)
        {
            var hostsPerPage = Program.Settings.HostsPerPage();
            var currentIndex = 0;
            var currentPage = 0;
            var targets = options.Select(c => creator(c)).ToList();
            CreateSpace();
            while (currentIndex <= targets.Count() - 1)
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
                    if (string.IsNullOrEmpty(target.command))
                    {
                        target.command = currentIndex.ToString();
                    }
                    Console.WriteLine($" {target.command}: {target.description}");
                    currentIndex++;
                }
            }
            var choice = RequestString(what);
            return targets.Where(t => string.Equals(t.command, choice)).Select(t => t.item).FirstOrDefault();
        }

        public class Choice
        {
            public static Choice<T> Create<T>(T item, string description = null)
            {
                {
                    var newItem = new Choice<T>(item);
                    if (!string.IsNullOrEmpty(newItem.description))
                    {
                        newItem.description = description;
                    }
                    return newItem;
                }
            }
        }

        public class Choice<T>
        {
            public Choice(T item)
            {
                this.item = item;
                this.description = item.ToString();
            }
            public string command { get; set; }
            public string description { get; set; }
            public T item { get; }
        }
    }
}
