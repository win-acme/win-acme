using PKISharp.WACS.DomainObjects;
using install = PKISharp.WACS.Plugins.InstallationPlugins;
using target = PKISharp.WACS.Plugins.TargetPlugins;
using store = PKISharp.WACS.Plugins.StorePlugins;
using dns = PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using http = PKISharp.WACS.Plugins.ValidationPlugins.Http;
using System.Collections.Generic;
using System.Linq;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Extensions;
using System.Globalization;
using PKISharp.WACS.Configuration;

namespace PKISharp.WACS.Services.Legacy
{
    class Importer
    {
        private readonly ILegacyRenewalService _legacyRenewal;
        private readonly IRenewalService _currentRenewal;
        private readonly ILogService _log;
        private readonly IInputService _input;
        private readonly TaskSchedulerService _currentTaskScheduler;
        private readonly LegacyTaskSchedulerService _legacyTaskScheduler;

        public Importer(ILogService log, IInputService input,
            ILegacyRenewalService legacyRenewal, IRenewalService currentRenewal, 
            LegacyTaskSchedulerService legacyTaskScheduler, TaskSchedulerService currentTaskScheduler)
        {
            _legacyRenewal = legacyRenewal;
            _currentRenewal = currentRenewal;
            _log = log;
            _input = input;
            _currentTaskScheduler = currentTaskScheduler;
            _legacyTaskScheduler = legacyTaskScheduler;
        }

        public void Import()
        {
            _log.Information("Legacy renewals {x}", _legacyRenewal.Renewals.Count().ToString());
            _log.Information("Current renewals {x}", _currentRenewal.Renewals.Count().ToString());
            foreach (LegacyScheduledRenewal legacyRenewal in _legacyRenewal.Renewals)
            {
                var converted = Convert(legacyRenewal);
                _currentRenewal.Import(converted);
            }
            _currentTaskScheduler.EnsureTaskScheduler();
            _legacyTaskScheduler.StopTaskScheduler();
        }

        public Renewal Convert(LegacyScheduledRenewal legacy)
        {
            // Note that history is not moved, so all imported renewals
            // will be due immediately. That's the ulimate test to see 
            // if they will actually work in the new ACMEv2 environment

            var ret = new Renewal();
            ConvertTarget(legacy, ret);
            ConvertValidation(legacy, ret);
            ConvertStore(legacy, ret);
            ConvertInstallation(legacy, ret);
            return ret;
        }

        public void ConvertTarget(LegacyScheduledRenewal legacy, Renewal ret)
        {
            if (string.IsNullOrEmpty(legacy.Binding.TargetPluginName))
            {
                switch (legacy.Binding.PluginName)
                {
                    case "IIS":
                        legacy.Binding.TargetPluginName = legacy.Binding.HostIsDns == false ? "IISSite" : "IISBinding";
                        break;
                    case "IISSiteServer":
                        legacy.Binding.TargetPluginName = "IISSites";
                        break;
                    case "Manual":
                        legacy.Binding.TargetPluginName = "Manual";
                        break;
                }
            }
            switch (legacy.Binding.TargetPluginName.ToLower())
            {
                case "iissite":
                    ret.TargetPluginOptions = new target.IISSiteOptions() {
                        CommonName = string.IsNullOrEmpty(legacy.Binding.CommonName) ? null : legacy.Binding.CommonName,
                        ExcludeBindings = legacy.Binding.ExcludeBindings.ParseCsv(),
                        SiteId = legacy.Binding.TargetSiteId ?? legacy.Binding.SiteId ?? 0
                    };
                    break;
                case "iissites":
                    ret.TargetPluginOptions = new target.IISSitesOptions()
                    {
                        CommonName = string.IsNullOrEmpty(legacy.Binding.CommonName) ? null : legacy.Binding.CommonName,
                        ExcludeBindings = legacy.Binding.ExcludeBindings.ParseCsv(),
                        SiteIds = legacy.Binding.Host.ParseCsv().Select(x => long.Parse(x)).ToList()
                    };
                    break;
                case "manual":
                    ret.TargetPluginOptions = new target.ManualOptions()
                    {
                        CommonName = string.IsNullOrEmpty(legacy.Binding.CommonName) ? legacy.Binding.Host : legacy.Binding.CommonName,
                        AlternativeNames = legacy.Binding.AlternativeNames
                    };
                    break;
                case "iisbinding":
                    ret.TargetPluginOptions = new target.IISBindingOptions()
                    {
                        Host = legacy.Binding.Host,
                        SiteId = (long)(legacy.Binding.TargetSiteId ?? legacy.Binding.SiteId)
                    };
                    break;
            }
        }

        public void ConvertValidation(LegacyScheduledRenewal legacy, Renewal ret)
        {
            // Configure validation
            switch (legacy.Binding.ValidationPluginName.ToLower())
            {
                case "dns-01.script":
                case "dns-01.dnsscript":
                    ret.ValidationPluginOptions = new dns.ScriptOptions()
                    {
                        ScriptConfiguration = legacy.Binding.DnsScriptOptions
                    };
                    break;
                case "dns-01.azure":
                    ret.ValidationPluginOptions = new dns.AzureOptions()
                    {
                        AzureConfiguration = legacy.Binding.DnsAzureOptions
                    };
                    break;
                case "http-01.ftp":
                    ret.ValidationPluginOptions = new http.FtpOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Warmup = legacy.Warmup,
                        Credential = new NetworkCredentialOptions(legacy.Binding.HttpFtpOptions.UserName, legacy.Binding.HttpFtpOptions.Password)
                    };
                    break;
                case "http-01.sftp":
                    ret.ValidationPluginOptions = new http.SftpOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Warmup = legacy.Warmup,
                        Credential = new NetworkCredentialOptions(legacy.Binding.HttpFtpOptions.UserName, legacy.Binding.HttpFtpOptions.Password)
                    };
                    break;
                case "http-01.webdav":
                    ret.ValidationPluginOptions = new http.WebDavOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Warmup = legacy.Warmup,
                        Credential = new NetworkCredentialOptions(legacy.Binding.HttpWebDavOptions.UserName, legacy.Binding.HttpWebDavOptions.Password)
                    };
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
                        SiteId = legacy.Binding.ValidationSiteId,
                        Warmup = legacy.Warmup
                    };
                    break;
            }
        }

        public void ConvertStore(LegacyScheduledRenewal legacy, Renewal ret)
        {           
            // Configure store
            if (!string.IsNullOrEmpty(legacy.CentralSslStore))
            {
                ret.StorePluginOptions = new store.CentralSslOptions()
                {
                    Path = legacy.CentralSslStore,
                    KeepExisting = legacy.KeepExisting == true
                };
            }
            else
            {
                ret.StorePluginOptions = new store.CertificateStorePluginOptions()
                {
                    StoreName = legacy.CertificateStore,
                    KeepExisting = legacy.KeepExisting == true
                };
            }
        }

        public void ConvertInstallation(LegacyScheduledRenewal legacy, Renewal ret)
        {
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
                        ret.InstallationPluginOptions.Add(new install.IISFtpOptions() {
                            SiteId = legacy.Binding.FtpSiteId.Value
                        });
                        break;
                    case "manual":
                        ret.InstallationPluginOptions.Add(new install.ScriptOptions() {
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
