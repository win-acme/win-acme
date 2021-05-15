using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Services
{
    public interface IArgumentsService
    {
        MainArguments MainArguments { get; }
        T? GetArguments<T>() where T : class, new();
    }
}