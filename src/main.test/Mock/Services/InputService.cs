using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    class InputService : IInputService
    {
        private readonly Queue<string> _inputs;
        private string GetNextInput() => _inputs.Dequeue();
        public InputService(List<string> inputs) => _inputs = new Queue<string>(inputs);

        public Task<TResult?> ChooseOptional<TSource, TResult>(
            string what,
            IEnumerable<TSource> options,
            Func<TSource, Choice<TResult?>> creator,
            string nullChoiceLabel) where TResult : class
        {
            var input = GetNextInput();
            var choices = options.Select(o => creator(o)).ToList();
            NumberChoices(choices.OfType<Choice>().ToList());
            if (string.Equals(nullChoiceLabel, input))
            {
                return Task.FromResult(default(TResult));
            }
            else
            {
                return Task.FromResult(choices.
                    FirstOrDefault(c => string.Equals(c.Command, input, StringComparison.InvariantCultureIgnoreCase))?.
                    Item);
            }
        }
        public Task<TResult> ChooseRequired<TSource, TResult>(
            string what,
            IEnumerable<TSource> options,
            Func<TSource, Choice<TResult>> creator)
        {
            var input = GetNextInput();
            return Task.FromResult(options.Select(o => creator(o)).
                First(c => string.Equals(c.Command, input, StringComparison.InvariantCultureIgnoreCase)).Item);
        }

        public string FormatDate(DateTime date) => "";
        public Task<bool> PromptYesNo(string message, bool defaultOption)
        {
            var input = GetNextInput();
            return Task.FromResult(string.Equals(input, "y", StringComparison.InvariantCultureIgnoreCase));
        }
        public Task<string?> ReadPassword(string what) => Task.FromResult<string?>(GetNextInput());
        public Task<string> RequestString(string what, bool multiline) => Task.FromResult(GetNextInput());
        public void Show(string? label, string? value = null, int level = 0) { }
        public Task<bool> Wait(string message = "") => Task.FromResult(true);
        public Task WritePagedList(IEnumerable<Choice> listItems) => Task.CompletedTask;
        public Task<TResult> ChooseFromMenu<TResult>(string what, List<Choice<TResult>> choices, Func<string, Choice<TResult>>? unexpected = null)
        {
            var input = GetNextInput();
            NumberChoices(choices.OfType<Choice>().ToList());
            var choice = choices.FirstOrDefault(c => string.Equals(c.Command, input, StringComparison.InvariantCultureIgnoreCase));
            if (choice == null && unexpected != null)
            {
                choice = unexpected(input);
            }
            if (choice != null)
            {
                return Task.FromResult(choice.Item);
            }
            throw new Exception();
        }
        internal void NumberChoices(List<Choice> choices)
        {
            foreach (var c in choices)
            {
                if (c.Command == null)
                {
                    c.Command = (choices.IndexOf(c) + 1).ToString();
                }
            }
        }

        public void CreateSpace() { }
    }
}
