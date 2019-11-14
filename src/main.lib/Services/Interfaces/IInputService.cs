using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public interface IInputService
    {
        Task<TResult> ChooseFromList<TSource, TResult>(string what, IEnumerable<TSource> options, Func<TSource, Choice<TResult>> creator, string nullChoiceLabel = null);
        Task<TResult> ChooseFromList<TResult>(string what, List<Choice<TResult>> choices);
        Task<bool> PromptYesNo(string message, bool defaultOption);
        Task<string> ReadPassword(string what);
        Task<string> RequestString(string what);
        Task<string> RequestString(string[] what);
        void Show(string label, string value = null, bool first = false, int level = 0);
        Task<bool> Wait(string message = "");
        Task WritePagedList(IEnumerable<Choice> listItems);
        string FormatDate(DateTime date);
    }


    public class Choice
    {
        public static Choice<T> Create<T>(T item,
            string description = null,
            string command = null,
            bool @default = false,
            bool disabled = false,
            ConsoleColor? color = null)
        {
            var newItem = new Choice<T>(item);
            if (!string.IsNullOrEmpty(description))
            {
                newItem.Description = description;
            }
            newItem.Command = command;
            newItem.Color = color;
            newItem.Disabled = disabled;
            newItem.Default = @default;
            return newItem;
        }

        public string Command { get; set; }
        public string Description { get; set; }
        public bool Default { get; set; }
        public bool Disabled { get; set; }
        public ConsoleColor? Color { get; set; }
    }

    public class Choice<T> : Choice
    {
        public Choice(T item)
        {
            Item = item;
            if (item != null)
            {
                Description = item.ToString();
            }
        }
        public T Item { get; }
    }
}