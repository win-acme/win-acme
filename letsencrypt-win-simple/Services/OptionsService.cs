using System;
using CommandLine;

namespace PKISharp.WACS.Services
{
    public class OptionsService : IOptionsService
    {
        private ILogService _log;
        public Options Options { get; set; }

        public OptionsService(ILogService log, string[] args)
        {
            _log = log;
            if (ParseCommandLine(args))
            {
                if (Options.Verbose)
                {
                    _log.SetVerbose();
                }
                if (Options.Test)
                {
                    SetTestParameters();
                }
            }
        }

        private void SetTestParameters()
        {
            Options.BaseUri = "https://acme-staging.api.letsencrypt.org/";
            //Log.SetVerbose();
            _log.Debug("Test parameter set: {BaseUri}", Options.BaseUri);
        }

        private bool ParseCommandLine(string[] args)
        {
            try
            {
                var commandLineParseResult = Parser.Default.ParseArguments<Options>(args).
                    WithNotParsed((errors) =>
                    {
                        foreach (var error in errors)
                        {
                            switch (error.Tag)
                            {
                                case ErrorType.UnknownOptionError:
                                    var unknownOption = (UnknownOptionError)error;
                                    var token = unknownOption.Token.ToLower();
                                    _log.Error("Unknown argument: {tag}", token);
                                    break;
                                case ErrorType.MissingValueOptionError:
                                    var missingValue = (MissingValueOptionError)error;
                                    token = missingValue.NameInfo.NameText;
                                    _log.Error("Missing value: {tag}", token);
                                    break;
                                case ErrorType.HelpRequestedError:
                                case ErrorType.VersionRequestedError:
                                    break;
                                default:
                                    _log.Error("Argument error: {tag}", error.Tag);
                                    break;
                            }
                        }
                    }).
                    WithParsed((result) =>
                    {
                        Options = result;
                        _log.Debug("Options: {@Options}", Options);
                    });
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed while parsing options.");
                throw;
            }
            return Options != null;
        }

        public string TryGetOption(string providedValue, IInputService input, string what, bool secret = false)
        {
            return TryGetOption(providedValue, input, new[] { what }, secret);
        }

        public string TryGetOption(string providedValue, IInputService input, string[] what, bool secret = false)
        {
            if (!string.IsNullOrWhiteSpace(providedValue)) return providedValue;
            if (secret) return input.ReadPassword(what[0]);
            return input.RequestString(what);
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
