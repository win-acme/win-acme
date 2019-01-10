using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IInputService
    {
        TResult ChooseFromList<TSource, TResult>(string what, IEnumerable<TSource> options, Func<TSource, Choice<TResult>> creator, bool allowNull);
        TResult ChooseFromList<TResult>(string what, List<Choice<TResult>> choices, bool allowNull);
        bool PromptYesNo(string message);
        string ReadPassword(string what);
        string RequestString(string what);
        string RequestString(string[] what);
        void Show(string label, string value = null, bool first = false, int level = 0);
        bool Wait();
        void WritePagedList(IEnumerable<Choice> listItems);
    }
}