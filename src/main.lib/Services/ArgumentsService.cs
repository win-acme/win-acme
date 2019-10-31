using PKISharp.WACS.Configuration;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class ArgumentsService : IArgumentsService
    {
        private readonly ILogService _log;
        private readonly ArgumentsParser _parser;

        public MainArguments MainArguments { get; private set; }

        public ArgumentsService(ILogService log, ArgumentsParser parser)
        {
            _log = log;
            _parser = parser;
            if (parser.Validate())
            {
                MainArguments = parser.GetArguments<MainArguments>();
            }
        }

        public T GetArguments<T>() where T : new() => _parser.GetArguments<T>();

        public async Task<string> TryGetArgument(string providedValue, IInputService input, string what, bool secret = false) => await TryGetArgument(providedValue, input, new[] { what }, secret);

        public async Task<string> TryGetArgument(string providedValue, IInputService input, string[] what, bool secret = false)
        {
            if (!string.IsNullOrWhiteSpace(providedValue))
            {
                return providedValue;
            }

            if (secret)
            {
                return await input.ReadPassword(what[0]);
            }

            var raw = await input.RequestString(what);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }
            else
            {
                return raw;
            }
        }

        public string TryGetRequiredArgument(string optionName, string providedValue)
        {
            if (string.IsNullOrWhiteSpace(providedValue))
            {
                _log.Error("Option --{optionName} not provided", optionName.ToLower());
                throw new Exception($"Option --{optionName.ToLower()} not provided");
            }
            return providedValue;
        }

        public void ShowHelp() => _parser.ShowArguments();

        public bool Active() => _parser.Active();

        public void ShowCommandLine() => _parser.ShowCommandLine();

        /// <summary>
        /// Is the command (e.g. --cancel or --renew)
        /// filtered for specific renewals
        /// </summary>
        /// <returns></returns>
        public bool HasFilter()
        {
            return
                !string.IsNullOrEmpty(MainArguments.Id) ||
                !string.IsNullOrEmpty(MainArguments.FriendlyName);
        }
    }
}
