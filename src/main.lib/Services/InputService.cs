using PKISharp.WACS.Configuration.Arguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class InputService : IInputService
    {
        private readonly MainArguments _arguments;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private const string _cancelCommand = "C";
        private bool _dirty;

        public InputService(MainArguments arguments, ISettingsService settings, ILogService log)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
        }

        private void Validate(string what)
        {
            if (_arguments.Renew && !_arguments.Test)
            {
                throw new Exception($"User input '{what}' should not be needed in --renew mode.");
            }
        }

        public void CreateSpace()
        {
            if (_log.Dirty || _dirty)
            {
                _log.Dirty = false;
                _dirty = false;
                WriteLine();
            }
        }

        private const string Black = "\u001b[40m";
        private const string Reset = "\u001b[0m";
        private static void WriteLine(string? text = "", ConsoleColor? color = null)
        {
            text ??= "";
            var size = Console.WindowWidth - 1;
            if (size != Console.CursorLeft + 1)
            {
                size -= Console.CursorLeft;
            }
            if (size < text.Length)
            {
                size = Console.WindowWidth + size;
            }
            if (size < 0)
            {
                size = text.Length;
            }
            Write($"{Black}{text.PadRight(size)}{Reset}\n", color);
        }

        private static void Write(string? text = "", ConsoleColor? color = null)
        {
            text ??= "";
            if (color != null)
            {
                Console.ForegroundColor = color.Value;
            }
            Console.Write($"{Black}{text}{Reset}");
            Console.ResetColor();
        }

        public Task<bool> Continue(string message = "Press <Space> to continue...")
        {
            Validate(message);
            CreateSpace();
            Write($" {message} ");
            while (true)
            {
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.Spacebar:
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop);
                        return Task.FromResult(true);
                }
            }
        }

        public Task<bool> Wait(string message = "Press <Enter> to continue...")
        {
            Validate(message);
            CreateSpace();
            Write($" {message} ");
            while (true)
            {
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.Enter:
                        WriteLine();
                        WriteLine();
                        return Task.FromResult(true);
                    case ConsoleKey.Escape:
                        WriteLine();
                        WriteLine();
                        return Task.FromResult(false);
                    default:
                        _log.Verbose("Unexpected key {key} pressed", response.Key);
                        continue;
                }
            }
        }

        public void Show(string? label, string? value, int level = 0)
        {
            var offset = 0;
            var hasLabel = !string.IsNullOrEmpty(label);
            if (hasLabel)
            {
                if (level > 0)
                {
                    label = string.Join("", Enumerable.Repeat("  ", level)) + $"- {label}";
                }
                offset = Math.Max(20, label!.Length + 1);
                Write(label, ConsoleColor.White);
            }

            if (!string.IsNullOrWhiteSpace(value))
            {
                if (hasLabel)
                {
                    Write(":");
                }
                WriteMultiline(offset, value);
            }
            else
            {
                if (!Console.IsOutputRedirected)
                {
                    Console.SetCursorPosition(15, Console.CursorTop);
                }
                WriteLine($"-----------------------------------------------------------------");
            }

            _dirty = true;
        }

        private static void WriteMultiline(int startPos, string value)
        {
            var step = 79 - startPos;
            var sentences = value.Split('\n');
            foreach (var sentence in sentences)
            {
                var pos = 0;
                var words = sentence.Split(' ');
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
                            line += words[pos++] + " ";
                        }
                    }
                    if (!Console.IsOutputRedirected)
                    {
                        Console.SetCursorPosition(startPos, Console.CursorTop);
                    }
                    WriteLine($" {line.TrimEnd()}");
                }
            }
        }

        public async Task<int?> RequestInt(string what)
        {
            var str = await RequestString(what);
            if (int.TryParse(str, out var ret))
            {
                return ret;
            }
            else
            {
                _log.Warning("Invalid number: {ret}", str);
                return null;
            }
        }

        public Task<string> RequestString(string what, bool multiline = false)
        {
            Validate(what);
            CreateSpace();
            Write($" {what}: ", ConsoleColor.Green);

            // Copied from http://stackoverflow.com/a/16638000
            var bufferSize = 16384;
            var inputStream = Console.OpenStandardInput(bufferSize);
            Console.SetIn(new StreamReader(inputStream, Console.InputEncoding, false, bufferSize));

            int top = default;
            int left = default;
            if (!Console.IsOutputRedirected)
            {
                top = Console.CursorTop;
                left = Console.CursorLeft;
            }

            var ret = new StringBuilder();
            do
            {
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                {
                    break;
                }
                ret.AppendLine(line);
            }
            while (multiline);

            var answer = ret.ToString();
            if (string.IsNullOrWhiteSpace(answer))
            {
                if (!Console.IsOutputRedirected)
                {
                    Console.SetCursorPosition(left, top);
                }
                WriteLine("<Enter>");
                WriteLine();
                return Task.FromResult(string.Empty);
            }
            else
            {
                WriteLine();
                return Task.FromResult(answer.Trim());
            }
        }

        public Task<bool> PromptYesNo(string message, bool defaultChoice)
        {
            Validate(message);
            CreateSpace();
            Write($" {message} ", ConsoleColor.Green);
            if (defaultChoice)
            {
                Write($"(y*/n) ", ConsoleColor.Yellow);
            }
            else
            {
                Write($"(y/n*) ", ConsoleColor.Yellow);
            }
            while (true)
            {
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.Enter:
                        WriteLine($"- <Enter>");
                        WriteLine();
                        return Task.FromResult(defaultChoice);
                }
                switch (response.KeyChar.ToString().ToLower())
                {
                    case "y":
                        WriteLine("- yes");
                        WriteLine();
                        return Task.FromResult(true);
                    case "n":
                        WriteLine("- no");
                        WriteLine();
                        return Task.FromResult(false);
                }
            }
        }

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        public Task<string?> ReadPassword(string what)
        {
            Validate(what);
            CreateSpace();
            Write($" {what}: ", ConsoleColor.Green);
            var password = new StringBuilder();
            try
            {
                var info = Console.ReadKey(true);
                while (info.Key != ConsoleKey.Enter)
                {
                    if (info.Key != ConsoleKey.Backspace)
                    {
                        Write("*");
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
                            Write(" ");
                            // move the cursor to the left by one character again
                            Console.SetCursorPosition(pos - 1, Console.CursorTop);
                        }
                    }
                    info = Console.ReadKey(true);
                }
                // add a new line because user pressed enter at the end of their password
                WriteLine();
                _dirty = true;
                _log.Dirty = true;
            }
            catch (Exception ex)
            {
                _log.Error("Error reading Password: {@ex}", ex);
            }

            // Return null instead of emtpy string to save storage
            var ret = password.ToString();
            if (string.IsNullOrEmpty(ret))
            {
                return Task.FromResult<string?>(default);
            }
            else
            {
                return Task.FromResult<string?>(ret);
            }
        }

        /// <summary>
        /// Version of the picker where null may be returned
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="what"></param>
        /// <param name="options"></param>
        /// <param name="creator"></param>
        /// <param name="nullLabel"></param>
        /// <returns></returns>
        public async Task<TResult?> ChooseOptional<TSource, TResult>(
            string what, IEnumerable<TSource> options,
            Func<TSource, Choice<TResult?>> creator,
            string nullLabel) where TResult : class
        {
            var baseChoices = options.Select(creator).ToList();
            if (!baseChoices.Any(x => !x.Disabled))
            {
                _log.Warning("No options available");
                return default;
            }
            var defaults = baseChoices.Where(x => x.Default);
            var cancel = Choice.Create(default(TResult), nullLabel, _cancelCommand);
            if (!defaults.Any())
            {
                cancel.Command = "<Enter>";
                cancel.Default = true;
            }
            baseChoices.Add(cancel);
            return await ChooseFromMenu(what, baseChoices);
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="targets"></param>
        public async Task<T> ChooseRequired<S, T>(
            string what, 
            IEnumerable<S> options, 
            Func<S, Choice<T>> creator) 
        {
            var baseChoices = options.Select(creator).ToList();
            if (!baseChoices.Any(x => !x.Disabled))
            {
                throw new Exception("No options available for required choice");
            }
            return await ChooseFromMenu(what, baseChoices);
        }

        /// <summary>
        /// Print a (paged) list of choices for the user to choose from
        /// </summary>
        /// <param name="choices"></param>
        public async Task<T> ChooseFromMenu<T>(string what, List<Choice<T>> choices, Func<string, Choice<T>>? unexpected = null)
        {
            if (!choices.Any())
            {
                throw new Exception("No options available");
            }
            var defaults = choices.Where(x => x.Default);
            if (defaults.Count() > 1)
            {
                throw new Exception("Multiple defaults provided");
            }
            else if (defaults.Count() == 1 && defaults.First().Disabled)
            {
                throw new Exception("Default option is disabled");
            }
            var duplicates = choices.
                Where(c => c.Command != null).
                GroupBy(c => c.Command).
                Where(g => g.Count() > 1);
            if (duplicates.Any())
            {
                throw new Exception($"Duplicate command: {duplicates.First().Key}");
            }

            await WritePagedList(choices);

            Choice<T>? selected = null;
            do
            {
                var choice = await RequestString(what);
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

                    selected ??= choices.
                            Where(t => string.Equals(t.Description, choice, StringComparison.InvariantCultureIgnoreCase)).
                            FirstOrDefault();

                    if (selected != null && selected.Disabled)
                    {
                        var disabledReason = selected.DisabledReason ?? "Run as Administator to enable all features.";
                        _log.Warning($"The option you have chosen is currently disabled. {disabledReason}");
                        selected = null;
                    }

                    if (selected == null && unexpected != null)
                    {
                        selected = unexpected(choice);
                    }
                }
            } while (selected == null);
            return selected.Item;
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="listItems"></param>
        public async Task WritePagedList(IEnumerable<Choice> listItems)
        {
            var currentIndex = 0;
            var currentPage = 0;
            CreateSpace();
            if (!listItems.Any())
            {
                WriteLine($" [empty] ");
                WriteLine();
                return;
            }

            while (currentIndex <= listItems.Count() - 1)
            {
                // Paging
                if (currentIndex > 0)
                {
                    if (await Continue())
                    {
                        currentPage += 1;
                    }
                    else
                    {
                        return;
                    }
                }
                var page = listItems.
                    Skip(currentPage * _settings.UI.PageSize).
                    Take(_settings.UI.PageSize);
                foreach (var target in page)
                {
                    target.Command ??= (currentIndex + 1).ToString();

                    if (!string.IsNullOrEmpty(target.Command))
                    {
                        Write($" {target.Command}: ", target.Default ?
                            ConsoleColor.Green :
                            target.Disabled ?
                                ConsoleColor.DarkGray :
                                ConsoleColor.White);
                    }
                    else
                    {
                        Write($" * ");
                    }

                    WriteLine(target.Description, 
                        target.Disabled ? ConsoleColor.DarkGray : 
                        target.Color.HasValue ? target.Color.Value : null);
                    currentIndex++;
                }
            }
            WriteLine();
        }

        public string FormatDate(DateTime date) => date.ToString(_settings.UI.DateFormat);
    }

}
