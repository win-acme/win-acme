using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Services
{
    public interface IOptionsService
    {
        MainArguments MainArguments { get; }
        T GetArguments<T>() where T : new();
        string TryGetOption(string providedValue, IInputService input, string what, bool secret = false);
        string TryGetOption(string providedValue, IInputService input, string[] what, bool secret = false);
        string TryGetRequiredOption(string optionName, string providedValue);
        long? TryGetLong(string optionName, string providedValue);
        void ShowHelp();
    }
}