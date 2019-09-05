using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IInputService
    {
        TResult ChooseFromList<TSource, TResult>(string what, IEnumerable<TSource> options, Func<TSource, Choice<TResult>> creator, string nullChoiceLabel = null);
        TResult ChooseFromList<TResult>(string what, List<Choice<TResult>> choices);
        bool PromptYesNo(string message, bool defaultOption);
        string ReadPassword(string what);
        string RequestString(string what);
        string RequestString(string[] what);
        void Show(string label, string value = null, bool first = false, int level = 0);
        bool Wait(string message = "");
        void WritePagedList(IEnumerable<Choice> listItems);
    }


    public class Choice
    {
        public static Choice<T> Create<T>(T item,
            string description = null,
            string command = null,
            bool @default = false,
            ConsoleColor? color = null)
        {
            var newItem = new Choice<T>(item);
            if (!string.IsNullOrEmpty(description))
            {
                newItem.Description = description;
            }
            newItem.Command = command;
            newItem.Color = color;
            newItem.Default = @default;
            return newItem;
        }

        public string Command { get; set; }
        public string Description { get; set; }
        public bool Default { get; set; }
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