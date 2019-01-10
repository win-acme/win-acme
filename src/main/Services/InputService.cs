using PKISharp.WACS.Clients.IIS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PKISharp.WACS.Services
{
    public class InputService : IInputService
    {
        private IOptionsService _options;
        private ILogService _log;
        private const string _cancelCommand = "C";
        private readonly int _pageSize;
        private bool _dirty;

        public InputService(IOptionsService options, ILogService log, ISettingsService settings)
        {
            _log = log;
            _options = options;
            _pageSize = settings.HostsPerPage;
        }

        private void Validate(string what)
        {
            if (_options.MainArguments.Renew && !_options.MainArguments.Test)
            {
                throw new Exception($"User input '{what}' should not be needed in --renew mode.");
            }
        }

        protected void CreateSpace(bool force = false)
        {
            if (_log.Dirty || _dirty)
            {
                _log.Dirty = false;
                _dirty = false;
                Console.WriteLine();
            }
            else if (force)
            {
                Console.WriteLine();
            }
        }

        public bool Wait()
        {
            var message = "Press enter to continue...";
            Validate(message);
            CreateSpace();
            Console.Write($" {message} ");
            while (true)
            {
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        Console.WriteLine();
                        return true;
                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        Console.WriteLine();
                        return false;
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

        public void Show(string label, string value, bool first = false, int level = 0)
        {
            if (first)
            {
                CreateSpace();
            }
            Console.ForegroundColor = ConsoleColor.White;
            if (level > 0)
            {
                Console.Write($"  - {label}");
            }
            else
            {
                Console.Write($" {label}");
            }
            Console.ResetColor();
            if (!string.IsNullOrWhiteSpace(value))
            {
                Console.Write(":");
                Console.SetCursorPosition(20, Console.CursorTop);
                Console.WriteLine($" {value}");
            }
            else
            {
                Console.SetCursorPosition(15, Console.CursorTop);
                Console.WriteLine($"------------------------------------------------------------------------------------");
            }

            _dirty = true;
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
            var bufferSize = 16384;
            var inputStream = Console.OpenStandardInput(bufferSize);
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
                // add another new line to keep a clean break with following log messages
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                _log.Error("Error reading Password: {@ex}", ex);
            }

            // Return null instead of emtpy string to save storage
            var ret = password.ToString();
            if (string.IsNullOrEmpty(ret))
            {
                return null;
            }
            else
            {
                return ret;
            }
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="targets"></param>
        public T ChooseFromList<S, T>(string what, IEnumerable<S> options, Func<S, Choice<T>> creator, bool allowNull)
        {
            return ChooseFromList(what, options.Select(creator).ToList(), allowNull);
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="choices"></param>
        public T ChooseFromList<T>(string what, List<Choice<T>> choices, bool allowNull)
        {
            if (!choices.Any())
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
                    Where(t => string.Equals(t.Command, choice, StringComparison.InvariantCultureIgnoreCase)).
                    FirstOrDefault();
            } while (selected == null);
            return selected.Item;
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
                Console.WriteLine($" [empty] ");
                Console.WriteLine();
                return;
            }

            while (currentIndex <= listItems.Count() - 1)
            {
                // Paging
                if (currentIndex > 0)
                {
                    if (Wait())
                    {
                        currentPage += 1;
                    } 
                    else
                    {
                        return;
                    }
                }
                var page = listItems.Skip(currentPage * _pageSize).Take(_pageSize);
                foreach (var target in page)
                {
                    if (target.Command == null)
                    {
                        target.Command = (currentIndex + 1).ToString();
                    }
                    if (!string.IsNullOrEmpty(target.Command))
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($" {target.Command}: ");
                        Console.ResetColor();
                    }
                    else
                    {
                        Console.Write($" * ");
                    }
                    if (target.Color.HasValue)
                    {
                        Console.ForegroundColor = target.Color.Value;
                    }
                    Console.WriteLine(target.Description);
                    Console.ResetColor();
                    currentIndex++;
                }
            }
            Console.WriteLine();
        }
    }

    public class Choice
    {
        public static Choice Create(string description = null, string command = null)
        {
            return Create<object>(null, description, command, null);
        }

        public static Choice<T> Create<T>(T item, string description = null, string command = null, ConsoleColor? color = null)
        {
            var newItem = new Choice<T>(item);
            if (!string.IsNullOrEmpty(description))
            {
                newItem.Description = description;
            }
            newItem.Command = command;
            newItem.Color = color;
            return newItem;
        }

        public string Command { get; set; }
        public string Description { get; set; }
        public ConsoleColor? Color { get; set; }
    }

    public class Choice<T> : Choice
    {
        public Choice(T item)
        {
            this.Item = item;
            if (item != null)
            {
                this.Description = item.ToString();
            }
        }
        public T Item { get; }
    }
}
