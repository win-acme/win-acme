using PKISharp.WACS.Configuration;
using System;

namespace PKISharp.WACS.Services
{
    public class ArgumentsService : IArgumentsService
    {
        private ILogService _log;
        private ArgumentsParser _parser;

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

        public T GetArguments<T>() where T : new()
        {
            return _parser.GetArguments<T>();
        }

        public string TryGetArgument(string providedValue, IInputService input, string what, bool secret = false)
        {
            return TryGetArgument(providedValue, input, new[] { what }, secret);
        }

        public string TryGetArgument(string providedValue, IInputService input, string[] what, bool secret = false)
        {
            if (!string.IsNullOrWhiteSpace(providedValue)) return providedValue;
            if (secret) return input.ReadPassword(what[0]);
            var raw = input.RequestString(what);
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

        public void ShowHelp()
        {
            _parser.ShowArguments();
        }

        public bool Active()
        {
            return _parser.Active();
        }

        public void ShowCommandLine()
        {
            _parser.ShowCommandLine();
        }

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
