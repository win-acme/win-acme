using PKISharp.WACS.Configuration;
using PKISharp.WACS.Extensions;
using System;

namespace PKISharp.WACS.Services
{
    public class OptionsService : IOptionsService
    {
        private ILogService _log;
        private ArgumentsParser _parser;

        public MainArguments MainArguments { get; private set; }

        public OptionsService(ILogService log, ArgumentsParser parser)
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

        public string TryGetOption(string providedValue, IInputService input, string what, bool secret = false)
        {
            return TryGetOption(providedValue, input, new[] { what }, secret);
        }

        public string TryGetOption(string providedValue, IInputService input, string[] what, bool secret = false)
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

        public string TryGetRequiredOption(string optionName, string providedValue)
        {
            if (string.IsNullOrWhiteSpace(providedValue))
            {
                _log.Error("Option --{optionName} not provided", optionName.ToLower());
                throw new Exception($"Option --{optionName.ToLower()} not provided");
            }
            return providedValue;
        }

        public long? TryGetLong(string optionName, string providedValue)
        {
            if (string.IsNullOrEmpty(providedValue))
            {
                return null;
            }
            if (long.TryParse(providedValue, out long output))
            {
                return output;
            }
            _log.Error("Invalid value for --{optionName}", optionName.ToLower());
            throw new Exception($"Invalid value for --{optionName.ToLower()}");
        }

        public void ShowHelp()
        {
            _parser.ShowArguments();
        }
    }
}
