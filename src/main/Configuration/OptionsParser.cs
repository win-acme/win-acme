using CommandLine;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Linq;

namespace PKISharp.WACS.Configuration
{
    class OptionsParser
    {
        public Options Options { get; private set; }
        private ILogService _log;

        public OptionsParser(ILogService log, string[] commandLine)
        {
            _log = log;
            if (ParseCommandLine(commandLine))
            {
                if (Options.Verbose)
                {
                    _log.SetVerbose();
                }
            }
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
                        var valid = result.Validate(_log);
                        if (valid)
                        {
                            Options = result;
                            _log.Debug("Options: {@Options}", Options);
                        }
                    });
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed while parsing options.");
                throw;
            }
            return Options != null;
        }

    }
}
