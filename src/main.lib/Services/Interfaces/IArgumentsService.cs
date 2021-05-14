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
        void ShowHelp();
        void ShowCommandLine();
    }
}