using PKISharp.WACS.Configuration;
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
        Task<string?> TryGetArgument(string? providedValue, IInputService input, string what, bool secret = false, bool multiline = false);
        Task<string?> TryGetArgument(string? providedValue, IInputService input, string[] what, bool secret = false, bool multiline = false);
        string TryGetRequiredArgument(string optionName, string? providedValue);
        void ShowHelp();
        void ShowCommandLine();
    }
}