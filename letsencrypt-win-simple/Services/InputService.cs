using LetsEncrypt.ACME.Simple.Clients;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LetsEncrypt.ACME.Simple.Services
{
    public class InputService
    {
        private Options _options;
        private ILogService _log;
        private const string _cancelCommand = "C";
        private int _pageSize;
        private bool _dirty;

        public InputService(Options options, ILogService log, int pageSize)
        {
            _log = log;
            _options = options;
            _pageSize = pageSize;
        }

        private void Validate(string what)
        {
            if (_options.Renew && !_options.Test)
            {
                throw new Exception($"User input '{what}' should not be needed in --renew mode.");
            }
        }

        protected void CreateSpace()
        {
            if (_log.Dirty || _dirty)
            {
                _log.Dirty = false;
                _dirty = false;
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

        public void Show(string label, string value, bool first = false)
        {
            if (first)
            {
                CreateSpace();
            }
            if (!string.IsNullOrEmpty(value))
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write($" {label}:");
                Console.ResetColor();
                Console.SetCursorPosition(20, Console.CursorTop);
                Console.WriteLine($" {value}");
                _dirty = true;
            }
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
            if (string.IsNullOrWhiteSpace(answer))
            {
                return string.Empty;
            }
            else
            {
                return answer.Trim();
            }
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
                // add another new line to keep a clean break with following log messages
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                _log.Error("Error reading Password: {@ex}", ex);
            }

            return password.ToString();
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="targets"></param>
        public T ChooseFromList<T>(string what, IEnumerable<T> options, Func<T, Choice<T>> creator, bool allowNull)
        {
            return ChooseFromList(what, options.Select((o) => creator(o)).ToList(), allowNull);
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="choices"></param>
        public T ChooseFromList<T>(string what, List<Choice<T>> choices, bool allowNull)
        {
            if (choices.Count() == 0)
            {
                if (allowNull) {
                    _log.Warning("No options available");
                    return default(T);
                } else {
                    throw new Exception("No options available for required choice");
                }
            }

            if (allowNull) {
                choices.Add(Choice.Create(default(T), "Cancel", _cancelCommand));
            }
            WritePagedList(choices);

            Choice<T> selected = null;
            do {
                var choice = RequestString(what);     
                selected = choices.
                    Where(t => string.Equals(t.command, choice, StringComparison.InvariantCultureIgnoreCase)).
                    FirstOrDefault();
            } while (selected == null);
            return selected.item;
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="listItems"></param>
        public void WritePagedList(IEnumerable<Choice> listItems)
        {
            var currentIndex = 0;
            var currentPage = 0;
            CreateSpace();
            if (listItems.Count() == 0)
            {
                Console.WriteLine($" [emtpy] ");
                Console.WriteLine();
                return;
            }

            while (currentIndex <= listItems.Count() - 1)
            {
                // Paging
                if (currentIndex > 0)
                {
                    Wait();
                    currentPage += 1;
                }
                var page = listItems.Skip(currentPage * _pageSize).Take(_pageSize);
                foreach (var target in page)
                {
                    if (target.command == null)
                    {
                        target.command = (currentIndex + 1).ToString();
                    }
                    if (!string.IsNullOrEmpty(target.command))
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($" {target.command}: ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write($" * ");
                    }
                    Console.WriteLine(target.description);
                    currentIndex++;
                }
            }
            Console.WriteLine();
        }

        /// <summary>
        /// Write banner during startup
        /// </summary>
        public void ShowBanner()
        {
            Console.WriteLine();
#if DEBUG
            var build = "DEBUG";
#else
            var build = "RELEASE";
#endif
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            _log.Information(true, "Let's Encrypt Windows Simple (LEWS)");
            _log.Information(true, "Software version {version} ({build})", version, build);
            if (IISClient.Version.Major > 0)
            {
                _log.Information("IIS version {version}", IISClient.Version);
            }
            else
            {
                _log.Information("IIS not detected");
            }
            _log.Information("ACME Server {ACME}", _options.BaseUri);
            _log.Information("Please report issues at {url}", "https://github.com/Lone-Coder/letsencrypt-win-simple");
            _log.Verbose("Verbose mode logging enabled");
            Console.WriteLine();
        }

        public class Choice
        {
            public static Choice Create(string description = null, string command = null)
            {
                return Create<object>(null, description, command);
            }

            public static Choice<T> Create<T>(T item, string description = null, string command = null)
            {
                {
                    var newItem = new Choice<T>(item);
                    if (!string.IsNullOrEmpty(description))
                    {
                        newItem.description = description;
                    }
                    newItem.command = command;
                    return newItem;
                }
            }

            public string command { get; set; }
            public string description { get; set; }
        }

        public class Choice<T> : Choice
        {
            public Choice(T item)
            {
                this.item = item;
                if (item != null) {
                    this.description = item.ToString();
                }
            }
            public T item { get; }
        }
    }
}
