using CommandLine;
using PKISharp.WACS.Clients.IIS;
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
                        var valid = true;
                        if (result.Renew)
                        {
                            if (
                                !string.IsNullOrEmpty(result.AzureClientId) ||
                                !string.IsNullOrEmpty(result.AzureResourceGroupName) ||
                                !string.IsNullOrEmpty(result.AzureSecret) ||
                                !string.IsNullOrEmpty(result.AzureSubscriptionId) ||
                                !string.IsNullOrEmpty(result.AzureTenantId) ||
                                !string.IsNullOrEmpty(result.CentralSslStore) ||
                                !string.IsNullOrEmpty(result.CertificateStore) ||
                                !string.IsNullOrEmpty(result.CommonName) ||
                                !string.IsNullOrEmpty(result.DnsCreateScript) ||
                                !string.IsNullOrEmpty(result.DnsDeleteScript) ||
                                !string.IsNullOrEmpty(result.ExcludeBindings) ||
                                !string.IsNullOrEmpty(result.FriendlyName) ||
                                !string.IsNullOrEmpty(result.FtpSiteId) ||
                                !string.IsNullOrEmpty(result.Host) ||
                                result.Installation.Count() > 0 ||
                                !string.IsNullOrEmpty(result.InstallationSiteId) ||
                                result.KeepExisting ||
                                result.ManualTargetIsIIS ||
                                !string.IsNullOrEmpty(result.Password) ||
                                !string.IsNullOrEmpty(result.PfxPassword) ||
                                !string.IsNullOrEmpty(result.Script) ||
                                !string.IsNullOrEmpty(result.ScriptParameters) ||
                                !string.IsNullOrEmpty(result.SiteId) ||
                                result.SSLIPAddress != IISClient.DefaultBindingIp ||
                                result.SSLPort != IISClient.DefaultBindingPort ||
                                !string.IsNullOrEmpty(result.Store) ||
                                !string.IsNullOrEmpty(result.Target) ||
                                !string.IsNullOrEmpty(result.UserName) ||
                                !string.IsNullOrEmpty(result.Validation) ||
                                result.ValidationPort != null ||
                                !string.IsNullOrEmpty(result.ValidationSiteId) ||
                                result.Warmup ||
                                !string.IsNullOrEmpty(result.WebRoot)
                            )
                            {
                                _log.Error("It's not possible to change properties during renewal. Edit the .json files or overwrite the renewal if you wish to change any settings.");
                                valid = false;
                            }
                        }
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
