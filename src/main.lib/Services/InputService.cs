using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace PKISharp.WACS.Services
{
    public class InputService : IInputService
    {
        private readonly IArgumentsService _arguments;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private const string _cancelCommand = "C";
        private bool _dirty;

        public InputService(IArgumentsService arguments, ISettingsService settings, ILogService log)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
        }

        private void Validate(string what)
        {
            if (_arguments.MainArguments.Renew && !_arguments.MainArguments.Test)
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

        public bool Wait(string message = "Press enter to continue...")
        {
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

        public void Show(string label, string value, bool newLine = false, int level = 0)
        {
            if (newLine)
            {
                CreateSpace();
            }
            var hasLabel = !string.IsNullOrEmpty(label);
            if (hasLabel)
            {
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
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (hasLabel)
                {
                    Console.Write(":");
                }
                WriteMultiline(hasLabel ? 20 : 0, value);
            }
            else
            {
                if (!Console.IsOutputRedirected)
                {
                    Console.SetCursorPosition(15, Console.CursorTop);
                }
                Console.WriteLine($"-----------------------------------------------------------------");
            }

            _dirty = true;
        }

        private void WriteMultiline(int startPos, string value)
        {
            var step = 80 - startPos;
            var pos = 0;
            var words = value.Split(' ');
            while (pos < words.Length)
            {
                var line = "";
                if (words[pos].Length + 1 >= step)
                {
                    line = words[pos++];
                }
                else
                {
                    while (pos < words.Length && line.Length + words[pos].Length + 1 < step)
                    {
                        line += " " + words[pos++];
                    }
                }
                if (!Console.IsOutputRedirected)
                {
                    Console.SetCursorPosition(startPos, Console.CursorTop);
                }
                Console.WriteLine($" {line}");
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
            var bufferSize = 16384;
            var inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));

            var top = Console.CursorTop;
            var left = Console.CursorLeft;
            answer = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(answer))
            {
                Console.SetCursorPosition(left, top);
                Console.WriteLine("<Enter>");
                Console.WriteLine();
                return string.Empty;
            }
            else
            {
                Console.WriteLine();
                return answer.Trim();
            }
        }

        public bool PromptYesNo(string message, bool defaultChoice)
        {
            Validate(message);
            CreateSpace();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($" {message} ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (defaultChoice)
            {
                Console.Write($"(y*/n) ");
            }
            else
            {
                Console.Write($"(y/n*) ");
            }
            Console.ResetColor();
            while (true)
            {
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.Y:
                        Console.WriteLine(" - yes");
                        Console.WriteLine();
                        return true;
                    case ConsoleKey.N:
                        Console.WriteLine(" - no");
                        Console.WriteLine();
                        return false;
                    case ConsoleKey.Enter:
                        Console.WriteLine($" - <Enter>");
                        Console.WriteLine();
                        return defaultChoice;
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
        public T ChooseFromList<S, T>(string what, IEnumerable<S> options, Func<S, Choice<T>> creator, string nullLabel = null)
        {
            var baseChoices = options.Select(creator).ToList();
            var allowNull = !string.IsNullOrEmpty(nullLabel);
            if (!baseChoices.Any())
            {
                if (allowNull)
                {
                    _log.Warning("No options available");
                    return default(T);
                }
                else
                {
                    throw new Exception("No options available for required choice");
                }
            }
            var defaults = baseChoices.Where(x => x.Default).Count();
            if (defaults > 1)
            {
                throw new Exception("Multiple defaults provided");
            }
            if (allowNull)
            {
                var cancel = Choice.Create(default(T), nullLabel, _cancelCommand);
                if (defaults == 0)
                {
                    cancel.Command = "<Enter>";
                    cancel.Default = true;
                }
                baseChoices.Add(cancel);
            }
            return ChooseFromList(what, baseChoices);
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="choices"></param>
        public T ChooseFromList<T>(string what, List<Choice<T>> choices)
        {
            if (!choices.Any())
            {
                throw new Exception("No options available");
            }

            WritePagedList(choices);

            Choice<T> selected = null;
            do
            {
                var choice = RequestString(what);
                if (string.IsNullOrWhiteSpace(choice))
                {
                    selected = choices.
                        Where(c => c.Default).
                        FirstOrDefault();
                }
                else
                {
                    selected = choices.
                        Where(t => string.Equals(t.Command, choice, StringComparison.InvariantCultureIgnoreCase)).
                        FirstOrDefault();
                }
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
                var page = listItems.Skip(currentPage * _settings.HostsPerPage).Take(_settings.HostsPerPage);
                foreach (var target in page)
                {
                    if (target.Command == null)
                    {
                        target.Command = (currentIndex + 1).ToString();
                    }
                    if (!string.IsNullOrEmpty(target.Command))
                    {
                        Console.ForegroundColor = target.Default ? ConsoleColor.Green : ConsoleColor.White;
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

        public string FormatDate(DateTime date) => date.ToString(_settings.FileDateFormat);
    }

}
