using PKISharp.WACS.Configuration;
using PKISharp.WACS.Configuration.Arguments;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class ArgumentsService : IArgumentsService
    {
        private readonly ILogService _log;
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

        public ArgumentsService(ILogService log, ArgumentsParser parser)
        {
            _log = log;
            _parser = parser;
        }

        public T? GetArguments<T>() where T : class, new() => _parser.GetArguments<T>();

        public async Task<string?> TryGetArgument(string? providedValue, IInputService input, string what, bool secret = false, bool multiline = false) => await TryGetArgument(providedValue, input, new[] { what }, secret, multiline);

        public async Task<string?> TryGetArgument(string? providedValue, IInputService input, string[] what, bool secret = false, bool multiline = false)
        {
            if (!string.IsNullOrWhiteSpace(providedValue))
            {
                return providedValue;
            }

            if (secret)
            {
                return await input.ReadPassword(what[0]);
            }

            var raw = await input.RequestString(what, multiline);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }
            else
            {
                return raw;
            }
        }

        public string TryGetRequiredArgument(string optionName, string? providedValue)
        {
            if (string.IsNullOrWhiteSpace(providedValue))
            {
                _log.Error("Option --{optionName} not provided", optionName.ToLower());
                throw new Exception($"Option --{optionName.ToLower()} not provided");
            }
            return providedValue;
        }

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
