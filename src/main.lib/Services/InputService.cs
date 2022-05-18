using Autofac.Features.AttributeFilters;
using PKISharp.WACS.Configuration.Arguments;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
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
        private Dialog _showDialog;
        private Dialog _loadDialog;
        private Dialog? _progressDialog;
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
            _showDialog = new Dialog() { Width = Dim.Fill(), Height = Dim.Fill() };
        }

        private void Validate(string what)
        {
            if (_arguments.Renew && !_arguments.Test)
            {
                throw new Exception($"User input '{what}' should not be needed in --renew mode.");
            }
        }

        private void HideShow()
        {
            if (_showDialog.SuperView != null)
            {
                _showDialog.SuperView.Remove(_showDialog);
            }
            if (_progressDialog?.SuperView != null)
            {
                _progressDialog.SuperView.Remove(_progressDialog);
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

        public Task<bool> Wait(string message = "Press <Enter> to continue...")
        {
            HideShow();
            var promise = new TaskCompletionSource<bool>();
            var dialog = default(Dialog);
            var yes = new Button("Continue", is_default: true);
            yes.Clicked += () =>
            {
                _inputFrame.Remove(dialog);
                promise.SetResult(true);
            };
            dialog = new Dialog(message, yes)
            {
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };
            _inputFrame.Add(dialog);
            dialog.SetFocus();
            Application.Refresh();
            return promise.Task;
        }

        public void Show(string? label, string? value, int level = 0)
        {
            if (_showDialog.SuperView == null)
            {
                _showDialog.RemoveAll();
                _displayFrame.Add(_showDialog);
            }
            var lastPos = _showDialog.Subviews.Last().Subviews.LastOrDefault();
            var y = lastPos != null ? lastPos.Y + 1 : 0;
            var labelText = new Label($"{label}: ") { Y = y };
            _showDialog.Add(labelText);
            _showDialog.Add(new Label(value) { Y = y, X = Pos.AnchorEnd(value?.Length ?? 0 + 1) });
        }

        public Task<string> RequestString(string what, bool multiline = false, bool secret = false)
        {
            HideShow();
            var promise = new TaskCompletionSource<string>();
            var dialog = new Dialog();
            var input = new TextField()
            {
                X = Pos.Center(),
                Y = Pos.Center(),
                Width = Dim.Percent(80),
                Height = Dim.Percent(30),
                Secret = secret
            };

            var done = new Button("Ok");
            var closeAndReturn = () =>
            {
                _inputFrame.Remove(dialog);
                HideShow();
                promise.TrySetResult(input.Text?.ToString() ?? string.Empty);
            };
            done.Clicked += closeAndReturn;
            input.KeyPress += (x) =>
            {
                if (x.KeyEvent.Key == Key.Enter)
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
            dialog.SetFocus();
            input.SetFocus();
            Application.Driver.SetCursorVisibility(CursorVisibility.VerticalFix);
            Application.MainLoop.Driver.Wakeup();
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
                HideShow();
                promise.SetResult(true);
            };
            var no = new Button("No", is_default: !defaultChoice);
            no.Clicked += () => {
                _inputFrame.Remove(dialog);
                HideShow();
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
        public async Task<string?> ReadPassword(string what) => await RequestString(what, secret: true);


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
            return await ChooseFromMenu(what, baseChoices).ConfigureAwait(false);
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
                    HideShow();
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
        Task<string> IInputService.RequestString(string what, bool multiline) => RequestString(what, multiline);
        void IInputService.Progress(string label) {
            if (_progressDialog == null)
            {
                _progressDialog = new Dialog() { Width = Dim.Fill(), Height = Dim.Fill() };
                _progressDialog.Add(new Label(label) { Y = Pos.Percent(20) });
                var x = new ProgressBar() { 
                    Y = Pos.Percent(30),
                    Width = Dim.Fill(),
                    Height = 1,
                    ColorScheme = Colors.Error
                };
                _progressDialog.Add(x);
                var t = new Timer(250);
                t.Elapsed += (c,e) => { 
                    x.Pulse(); 
                    Application.MainLoop.Driver.Wakeup();
                };
                t.Start();
            }
            if (_progressDialog.SuperView == null)
            {
                _displayFrame.Add(_progressDialog);
            }            
        }
    }

}
