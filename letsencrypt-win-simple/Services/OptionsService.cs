using System;
using CommandLine;

namespace PKISharp.WACS.Services
{
    public class OptionsService : IOptionsService
    {
        private ILogService _log;
        public Options Options { get; private set; }

        public OptionsService(ILogService log, Options options)
        {
            _log = log;
            Options = options;
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
            long output;
            if (long.TryParse(providedValue, out output))
            {
                return output;
            }
            _log.Error("Invalid value for --{optionName}", optionName.ToLower());
            throw new Exception($"Invalid value for --{optionName.ToLower()}");
        }
    }
}
