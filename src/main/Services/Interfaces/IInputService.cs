using System;
using System.Collections.Generic;

namespace PKISharp.WACS.Services
{
    public interface IInputService
    {
        T ChooseFromList<S, T>(string what, IEnumerable<S> options, Func<S, Choice<T>> creator, bool allowNull);
        T ChooseFromList<T>(string what, List<Choice<T>> choices, bool allowNull);
        bool PromptYesNo(string message);
        string ReadPassword(string what);
        string RequestString(string what);
        string RequestString(string[] what);
        void Show(string label, string value, bool first = false);
        void ShowBanner();
        bool Wait();
        void WritePagedList(IEnumerable<Choice> listItems);
    }
}