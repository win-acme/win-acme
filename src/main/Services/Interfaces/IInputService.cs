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
}