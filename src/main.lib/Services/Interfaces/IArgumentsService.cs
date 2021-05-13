using PKISharp.WACS.Configuration.Arguments;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public interface IArgumentsService
    {
        MainArguments MainArguments { get; }
        T? GetArguments<T>() where T : class, new();
        bool Active { get; }
        bool Valid { get; }
        bool HasFilter();
        [Obsolete]
        Task<string?> TryGetArgument(string? providedValue, IInputService input, string what, bool secret = false, bool multiline = false);

        [Obsolete]
        Task<string?> TryGetArgument(string? providedValue, IInputService input, string[] what, bool secret = false, bool multiline = false);

        [Obsolete]
        string TryGetRequiredArgument(string optionName, string? providedValue);
        void ShowHelp();
        void ShowCommandLine();
    }
}