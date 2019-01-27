using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Services
{
    public interface IArgumentsService
    {
        MainArguments MainArguments { get; }
        T GetArguments<T>() where T : new();
        string TryGetArgument(string providedValue, IInputService input, string what, bool secret = false);
        string TryGetArgument(string providedValue, IInputService input, string[] what, bool secret = false);
        string TryGetRequiredArgument(string optionName, string providedValue);
        void ShowHelp();
        void ShowCommandLine();
    }
}