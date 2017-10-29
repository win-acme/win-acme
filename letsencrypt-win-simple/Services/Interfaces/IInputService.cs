using System;
using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Services
{
    public interface IInputService
    {
        T ChooseFromList<T>(string what, IEnumerable<T> options, Func<T, Choice<T>> creator, bool allowNull);
        T ChooseFromList<T>(string what, List<Choice<T>> choices, bool allowNull);
        bool PromptYesNo(string message);
        string ReadPassword(string what);
        string RequestString(string what);
        string RequestString(string[] what);
        void Show(string label, string value, bool first = false);
        void ShowBanner();
        void Wait();
        void WritePagedList(IEnumerable<Choice> listItems);
    }
}