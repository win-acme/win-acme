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
        public bool Active => _parser.Active();
    }
}
