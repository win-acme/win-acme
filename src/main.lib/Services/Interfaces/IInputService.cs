using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public interface IInputService
    {
        Task<TResult?> ChooseOptional<TSource, TResult>(string what, IEnumerable<TSource> options, Func<TSource, Choice<TResult?>> creator, string nullChoiceLabel) where TResult : class;
        Task<TResult> ChooseRequired<TSource, TResult>(string what, IEnumerable<TSource> options, Func<TSource, Choice<TResult>> creator);
        Task<TResult> ChooseFromMenu<TResult>(string what, List<Choice<TResult>> choices);
        Task<bool> PromptYesNo(string message, bool defaultOption);
        Task<string?> ReadPassword(string what);
        Task<string> RequestString(string what);
        Task<string> RequestString(string[] what);
        void Show(string? label, string? value = null, bool first = false, int level = 0);
        Task<bool> Wait(string message = "Press <Enter> to continue");
        Task WritePagedList(IEnumerable<Choice> listItems);
        string FormatDate(DateTime date);
    }


    public class Choice
    {
        public static Choice<TItem> Create<TItem>(
            TItem item,
            string? description = null,
            string? command = null,
            bool @default = false,
            bool disabled = false,
            string? disabledReason = null,
            ConsoleColor? color = null)
        {
            var newItem = new Choice<TItem>(item);
            // Default description is item.ToString, but it may 
            // be overruled by the optional parameter here
            if (!string.IsNullOrEmpty(description))
            {
                newItem.Description = description;
            }
            newItem.Command = command;
            newItem.Color = color;
            newItem.Disabled = disabled;
            newItem.DisabledReason = disabledReason;
            newItem.Default = @default;
            return newItem;
        }

        public string? Command { get; set; }
        public string? Description { get; set; }
        public bool Default { get; set; }
        public bool Disabled { get; set; }
        public string? DisabledReason { get; set; }
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