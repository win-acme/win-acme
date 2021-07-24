using PKISharp.WACS.Clients.Acme;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using dns = PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using http = PKISharp.WACS.Plugins.ValidationPlugins.Http;
using install = PKISharp.WACS.Plugins.InstallationPlugins;
using store = PKISharp.WACS.Plugins.StorePlugins;
using target = PKISharp.WACS.Plugins.TargetPlugins;

namespace PKISharp.WACS.Services.Legacy
{
    internal class Importer
    {
        private readonly ILegacyRenewalService _legacyRenewal;
        private readonly IRenewalStore _currentRenewal;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly IInputService _input;
        private readonly TaskSchedulerService _currentTaskScheduler;
        private readonly LegacyTaskSchedulerService _legacyTaskScheduler;
        private readonly PasswordGenerator _passwordGenerator;
        private readonly AcmeClient _acmeClient;

        public Importer(
            ILogService log, ILegacyRenewalService legacyRenewal,
            ISettingsService settings, IRenewalStore currentRenewal,
            IInputService input,
            LegacyTaskSchedulerService legacyTaskScheduler,
            TaskSchedulerService currentTaskScheduler,
            PasswordGenerator passwordGenerator,
            AcmeClient acmeClient)
        {
            _legacyRenewal = legacyRenewal;
            _currentRenewal = currentRenewal;
            _log = log;
            _settings = settings;
            _input = input;
            _currentTaskScheduler = currentTaskScheduler;
            _legacyTaskScheduler = legacyTaskScheduler;
            _passwordGenerator = passwordGenerator;
            _acmeClient = acmeClient;
        }

        public async Task Import(RunLevel runLevel)
        {

            if (!_legacyRenewal.Renewals.Any())
            {
                _log.Warning("No legacy renewals found");
            }
            _log.Information("Legacy renewals {x}", _legacyRenewal.Renewals.Count().ToString());
            _log.Information("Current renewals {x}", _currentRenewal.Renewals.Count().ToString());
            _log.Information("Step {x}/3: convert renewals", 1);
            foreach (var legacyRenewal in _legacyRenewal.Renewals)
            {
                var converted = Convert(legacyRenewal);
                _currentRenewal.Import(converted);
            }
            _log.Information("Step {x}/3: create new scheduled task", 2);
            await _currentTaskScheduler.EnsureTaskScheduler(runLevel | RunLevel.Import);
            _legacyTaskScheduler.StopTaskScheduler();

            _log.Information("Step {x}/3: ensure ACMEv2 account", 3);
            await _acmeClient.GetAccount();
            var listCommand = "--list";
            var renewCommand = "--renew";
            if (runLevel.HasFlag(RunLevel.Interactive))
            {
                listCommand = "Manage renewals";
                renewCommand = "Run";
            }
            _input.CreateSpace();
            _input.Show(null,
                value: $"The renewals have now been imported into this new version " +
                "of the program. Nothing else will happen until new scheduled task is " +
                "first run *or* you trigger them manually. It is highly recommended " +
                $"to review the imported items with '{listCommand}' and to monitor the " +
                $"results of the first execution with '{renewCommand}'.");

        }

        public Renewal Convert(LegacyScheduledRenewal legacy)
        {
            // Note that history is not moved, so all imported renewals
            // will be due immediately. That's the ulimate test to see 
            // if they will actually work in the new ACMEv2 environment

            var ret = Renewal.Create(null, _settings.ScheduledTask, _passwordGenerator);
            ConvertTarget(legacy, ret);
            ConvertValidation(legacy, ret);
            ConvertStore(legacy, ret);
            ConvertInstallation(legacy, ret);
            ret.CsrPluginOptions = new RsaOptions();
            ret.LastFriendlyName = legacy.Binding?.Host;
            ret.History = new List<RenewResult> {
                new RenewResult("Imported") { }
            };
            return ret;
        }

        public void ConvertTarget(LegacyScheduledRenewal legacy, Renewal ret)
        {
            if (legacy.Binding == null)
            {
                throw new Exception("Cannot convert renewal with empty binding");
            }
            if (string.IsNullOrEmpty(legacy.Binding.TargetPluginName))
            {
                legacy.Binding.TargetPluginName = legacy.Binding.PluginName switch
                {
                    "IIS" => legacy.Binding.HostIsDns == false ? "IISSite" : "IISBinding",
                    "IISSiteServer" => "IISSites",
                    _ => "Manual",
                };
            }
            switch (legacy.Binding.TargetPluginName.ToLower())
            {
                case "iisbinding":
                    var options = new target.IISOptions();
                    if (!string.IsNullOrEmpty(legacy.Binding.Host))
                    {
                        options.IncludeHosts = new List<string>() { legacy.Binding.Host };
                    }
                    var siteId = legacy.Binding.TargetSiteId ?? legacy.Binding.SiteId ?? 0;
                    if (siteId > 0)
                    {
                        options.IncludeSiteIds = new List<long>() { siteId };
                    }
                    ret.TargetPluginOptions = options;
                    break;
                case "iissite":
                    options = new target.IISOptions();
                    if (!string.IsNullOrEmpty(legacy.Binding.CommonName))
                    {
                        options.CommonName = legacy.Binding.CommonName.ConvertPunycode();
                    }
                    siteId = legacy.Binding.TargetSiteId ?? legacy.Binding.SiteId ?? 0;
                    if (siteId > 0)
                    {
                        options.IncludeSiteIds = new List<long>() { siteId };
                    }
                    options.ExcludeHosts = legacy.Binding.ExcludeBindings.ParseCsv();
                    ret.TargetPluginOptions = options;
                    break;
                case "iissites":
                    options = new target.IISOptions();
                    if (!string.IsNullOrEmpty(legacy.Binding.CommonName))
                    {
                        options.CommonName = legacy.Binding.CommonName.ConvertPunycode();
                    }
                    if (!string.IsNullOrEmpty(legacy.Binding.Host))
                    {
                        options.IncludeSiteIds = legacy.Binding.Host.ParseCsv()!.Select(x => long.Parse(x)).ToList();
                    }
                    options.ExcludeHosts = legacy.Binding.ExcludeBindings.ParseCsv();
                    ret.TargetPluginOptions = options;
                    break;
                case "manual":
                    var manual = new target.ManualOptions()
                    {
                        CommonName = string.IsNullOrEmpty(legacy.Binding.CommonName) ? legacy.Binding.Host : legacy.Binding.CommonName.ConvertPunycode(),
                        AlternativeNames = legacy.Binding.AlternativeNames.Select(x => x.ConvertPunycode()).ToList()
                    };
                    if (!string.IsNullOrEmpty(manual.CommonName) && 
                        !manual.AlternativeNames.Contains(manual.CommonName))
                    {
                        manual.AlternativeNames.Insert(0, manual.CommonName);
                    }
                    ret.TargetPluginOptions = manual;
                    break;
            }
        }

        public void ConvertValidation(LegacyScheduledRenewal legacy, Renewal ret)
        {
            if (legacy.Binding == null)
            {
                throw new Exception("Cannot convert renewal with empty binding");
            }
            // Configure validation
            if (legacy.Binding.ValidationPluginName == null)
            {
                legacy.Binding.ValidationPluginName = "http-01.filesystem";
            }
            switch (legacy.Binding.ValidationPluginName.ToLower())
            {
                case "dns-01.script":
                case "dns-01.dnsscript":
                    ret.ValidationPluginOptions = new dns.ScriptOptions()
                    {
                        CreateScript = legacy.Binding.DnsScriptOptions?.CreateScript,
                        CreateScriptArguments = "{Identifier} {RecordName} {Token}",
                        DeleteScript = legacy.Binding.DnsScriptOptions?.DeleteScript,
                        DeleteScriptArguments = "{Identifier} {RecordName}"
                    };
                    break;
                case "dns-01.azure":
                    ret.ValidationPluginOptions = new CompatibleAzureOptions()
                    {
                        ClientId = legacy.Binding.DnsAzureOptions?.ClientId,
                        ResourceGroupName = legacy.Binding.DnsAzureOptions?.ResourceGroupName,
                        Secret = new ProtectedString(legacy.Binding.DnsAzureOptions?.Secret),
                        SubscriptionId = legacy.Binding.DnsAzureOptions?.SubscriptionId,
                        TenantId = legacy.Binding.DnsAzureOptions?.TenantId
                    };
                    break;
                case "http-01.ftp":
                    ret.ValidationPluginOptions = new http.FtpOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Credential = new NetworkCredentialOptions(legacy.Binding.HttpFtpOptions?.UserName, legacy.Binding.HttpFtpOptions?.Password)
                    };
                    break;
                case "http-01.sftp":
                    ret.ValidationPluginOptions = new http.SftpOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Credential = new NetworkCredentialOptions(legacy.Binding.HttpFtpOptions?.UserName, legacy.Binding.HttpFtpOptions?.Password)
                    };
                    break;
                case "http-01.webdav":
                    var options = new http.WebDavOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath
                    };
                    if (legacy.Binding.HttpWebDavOptions != null)
                    {
                        options.Credential = new NetworkCredentialOptions(
                            legacy.Binding.HttpWebDavOptions.UserName,
                            legacy.Binding.HttpWebDavOptions.Password);
                    }
                    ret.ValidationPluginOptions = options;
                    break;
                case "tls-sni-01.iis":
                    _log.Warning("TLS-SNI-01 validation was removed from ACMEv2, changing to SelfHosting. Note that this requires port 80 to be public rather than port 443.");
                    ret.ValidationPluginOptions = new http.SelfHostingOptions();
                    break;
                case "http-01.iis":
                case "http-01.selfhosting":
                    ret.ValidationPluginOptions = new http.SelfHostingOptions()
                    {
                        Port = legacy.Binding.ValidationPort
                    };
                    break;
                case "http-01.filesystem":
                default:
                    ret.ValidationPluginOptions = new http.FileSystemOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        SiteId = legacy.Binding.ValidationSiteId
                    };
                    break;
            }
        }

        public void ConvertStore(LegacyScheduledRenewal legacy, Renewal ret)
        {
            // Configure store
            if (!string.IsNullOrEmpty(legacy.CentralSslStore))
            {
                ret.StorePluginOptions.Add(new store.CentralSslOptions()
                {
                    Path = legacy.CentralSslStore,
                    KeepExisting = legacy.KeepExisting == true
                });
            }
            else
            {
                ret.StorePluginOptions.Add(new store.CertificateStoreOptions()
                {
                    StoreName = legacy.CertificateStore,
                    KeepExisting = legacy.KeepExisting == true
                });
            }
            ret.StorePluginOptions.Add(new store.PemFilesOptions()
            {
                Path = _settings.Cache.Path
            });
            ret.StorePluginOptions.Add(new store.PfxFileOptions()
            {
                Path = _settings.Cache.Path
            });
        }

        public void ConvertInstallation(LegacyScheduledRenewal legacy, Renewal ret)
        {
            if (legacy.Binding == null)
            {
                throw new Exception("Cannot convert renewal with empty binding");
            }
            if (legacy.InstallationPluginNames == null)
            {
                legacy.InstallationPluginNames = new List<string>();
                // Based on chosen target
                if (legacy.Binding.TargetPluginName == "IISSite" ||
                    legacy.Binding.TargetPluginName == "IISSites" ||
                    legacy.Binding.TargetPluginName == "IISBinding")
                {
                    legacy.InstallationPluginNames.Add("IIS");
                }

                // Based on command line
                if (!string.IsNullOrEmpty(legacy.Script) || !string.IsNullOrEmpty(legacy.ScriptParameters))
                {
                    legacy.InstallationPluginNames.Add("Manual");
                }

                // Cannot find anything, then it's no installation steps
                if (legacy.InstallationPluginNames.Count == 0)
                {
                    legacy.InstallationPluginNames.Add("None");
                }
            }
            foreach (var legacyName in legacy.InstallationPluginNames)
            {
                switch (legacyName.ToLower())
                {
                    case "iis":
                        ret.InstallationPluginOptions.Add(new install.IISWebOptions()
                        {
                            SiteId = legacy.Binding.InstallationSiteId,
                            NewBindingIp = legacy.Binding.SSLIPAddress,
                            NewBindingPort = legacy.Binding.SSLPort
                        });
                        break;
                    case "iisftp":
                        ret.InstallationPluginOptions.Add(new install.IISFtpOptions()
                        {
                            SiteId = legacy.Binding.FtpSiteId ?? 
                                legacy.Binding.InstallationSiteId ?? 
                                legacy.Binding.SiteId ?? 
                                0
                        });
                        break;
                    case "manual":
                        ret.InstallationPluginOptions.Add(new install.ScriptOptions()
                        {
                            Script = legacy.Script,
                            ScriptParameters = legacy.ScriptParameters
                        });
                        break;
                    case "none":
                        ret.InstallationPluginOptions.Add(new NullInstallationOptions());
                        break;
                }
            }
        }
    }
}
