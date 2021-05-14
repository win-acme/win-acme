using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;

namespace PKISharp.WACS.Services
{
    public class ArgumentsService : IArgumentsService
    {
        private readonly ArgumentsParser _parser;
        private MainArguments? _mainArguments;

        public MainArguments MainArguments
        {
            get
            {
                if (_mainArguments == null)
                {
                    _mainArguments = _parser.GetArguments<MainArguments>();
                    if (_mainArguments == null)
                    {
                        _mainArguments = new MainArguments();
                    }
                }
                return _mainArguments;
            }
        }

        public ArgumentsService(ArgumentsParser parser) => _parser = parser;
        public T? GetArguments<T>() where T : class, new() => _parser.GetArguments<T>();
        public void ShowHelp() => _parser.ShowArguments();
        public bool Valid => _parser.Validate();
        public bool Active => _parser.Active();
        public void ShowCommandLine() => _parser.ShowCommandLine();

        /// <summary>
        /// Is the command (e.g. --cancel or --renew)
        /// filtered for specific renewals
        /// </summary>
        /// <returns></returns>
        public bool HasFilter()
        {
            if (MainArguments == null)
            {
                return false;
            }
            return
                !string.IsNullOrEmpty(MainArguments.Id) ||
                !string.IsNullOrEmpty(MainArguments.FriendlyName);
        }
    }
}
