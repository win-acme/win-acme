using Autofac.Features.AttributeFilters;
using PKISharp.WACS.Configuration.Arguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;

namespace PKISharp.WACS.Services
{
    public class InputService : IInputService
    {
        private readonly MainArguments _arguments;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly FrameView _inputFrame;
        private readonly FrameView _displayFrame;
        private const string _cancelCommand = "C";
        private bool _dirty;

        public InputService(
            MainArguments arguments, 
            ISettingsService settings, 
            ILogService log,
            [KeyFilter("display")] FrameView displayFrame,
            [KeyFilter("input")] FrameView inputFrame)
        {
            _log = log;
            _arguments = arguments;
            _settings = settings;
            _inputFrame = inputFrame;
            _displayFrame = displayFrame; 
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
                Console.WriteLine();
            }
        }

        public Task<bool> Continue(string message = "Press <Space> to continue...")
        {
            Validate(message);
            CreateSpace();
            Console.Write($" {message} ");
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
            Console.Write($" {message} ");
            while (true)
            {
                var response = Console.ReadKey(true);
                switch (response.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        Console.WriteLine();
                        return Task.FromResult(true);
                    case ConsoleKey.Escape:
                        Console.WriteLine();
                        Console.WriteLine();
                        return Task.FromResult(false);
                    default:
                        _log.Verbose("Unexpected key {key} pressed", response.Key);
                        continue;
                }
            }
        }

        public void Show(string? label, string? value, int level = 0)
        {
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
                var offset = 0;
                if (hasLabel)
                {
                    offset = Math.Max(20, label!.Length + 2);
                    Console.Write(":");
                }
                WriteMultiline(offset, value);
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
                    Console.WriteLine($" {line.TrimEnd()}");
                }
            }
        }

        public Task<string> RequestString(string what, bool multiline = false)
        {
            var promise = new TaskCompletionSource<string>();
            var dialog = new Dialog();
            var input = new TextField()
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = Dim.Percent(80),
                Height = Dim.Percent(30)
            };

            var done = new Button("Ok");
            var closeAndReturn = () =>
            {
                _inputFrame.Remove(dialog);
                promise.TrySetResult(input.Text?.ToString() ?? string.Empty);
            }
;
            done.Clicked += closeAndReturn;
            input.KeyPress += (x) =>
            {
                if (x.KeyEvent.Key.HasFlag(Key.Enter))
                {
                    x.Handled = true;
                    closeAndReturn();
                }
            };
            //(e) => { 
            //    if (e.KeyEvent.Key.HasFlag(Key.Enter)) { 
            //        closeAndReturn(); 
            //    } 
            //};

            dialog = new Dialog(what, done)
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            dialog.Add(input);
            _inputFrame.Add(dialog);
            input.SetFocus();
            Application.Refresh();
            return promise.Task;

        }

        public Task<bool> PromptYesNo(string message, bool defaultChoice)
        {
            var promise = new TaskCompletionSource<bool>();
            var dialog = default(Dialog);
            var yes = new Button("Yes", is_default: defaultChoice);
            yes.Clicked += () =>
            {
                _inputFrame.Remove(dialog);
                promise.SetResult(true);
            };
            var no = new Button("No", is_default: !defaultChoice);
            no.Clicked += () => {
                _inputFrame.Remove(dialog);
                promise.SetResult(false);
            };
            dialog = new Dialog(message, yes, no)
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };       
            _inputFrame.Add(dialog);
            dialog.SetFocus();
            Application.Refresh();
            return promise.Task;
        }

        // Replaces the characters of the typed in password with asterisks
        // More info: http://rajeshbailwal.blogspot.com/2012/03/password-in-c-console-application.html
        public async Task<string?> ReadPassword(string what)
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
                return null;
            }
            else
            {
                return ret;
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
            if (defaults.Count() == 0)
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
        public Task<T> ChooseFromMenu<T>(string what, List<Choice<T>> choices, Func<string, Choice<T>>? unexpected = null)
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

            var promise = new TaskCompletionSource<T>();

            Choice<T>? selected = null;
            var dialog = new Dialog(what)
            {
                Y = Pos.Center(),
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            var items = new ListView(choices.Select(c => c.Description).ToList())
            {
                
                X = 0,
                Y = 0,
                Width = Dim.Fill(0),
                Height = Dim.Fill(0),
                AllowsMarking = false,
                ColorScheme = Colors.TopLevel,
                
            };
            items.OpenSelectedItem +=
                x =>
                {
                    selected = choices[items.SelectedItem];
                    _inputFrame.Remove(dialog);
                    Application.Refresh();
                    promise.SetResult(selected.Item);
                };
            dialog.Add(items);
            _inputFrame.Add(dialog);
            dialog.SetFocus();
            Application.Refresh();
            return promise.Task;
        }

        /// <summary>
        /// Print a (paged) list of targets for the user to choose from
        /// </summary>
        /// <param name="listItems"></param>
        public Task WritePagedList(IEnumerable<Choice> listItems)
        {
            var items = new ListView(listItems.Select(c => c.Description).ToList())
            {

                X = 0,
                Y = 0,
                Width = Dim.Fill(0),
                Height = Dim.Fill(0),
                AllowsMarking = false,
                ColorScheme = Colors.TopLevel,

            };

            _displayFrame.Add(items);
            Application.Refresh();
            return Task.CompletedTask;


        }

        public string FormatDate(DateTime date) => date.ToString(_settings.UI.DateFormat);
    }

}
