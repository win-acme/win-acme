using System.Collections.Generic;
using LetsEncryptWinSimple.Core.Configuration;

namespace LetsEncryptWinSimple.Core.Interfaces
{
    public interface IConsoleService
    {
        string ReadCommandFromConsole();
        bool PromptYesNo(string message);
        void PromptEnter(string message = "Press enter to continue.");
        void WriteLine(string message);
        void Write(string message);
        void WriteError(string message);
        string ReadLine();
        void PrintMenuForPlugins();
        void WriteQuitCommandInformation();
        string[] GetSanNames();
        string ReadPassword();
        void HandleMenuResponseForPlugins(List<Target> targets, string command);
        void WriteBindings(List<Target> targets);
    }
}