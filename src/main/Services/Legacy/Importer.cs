using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using install = PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.StorePlugins;
using dns = PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using http = PKISharp.WACS.Plugins.ValidationPlugins.Http;
using System.Collections.Generic;
using System.Linq;
using PKISharp.WACS.Plugins.Base.Factories.Null;

namespace PKISharp.WACS.Services.Legacy
{
    class Importer
    {
        private readonly ILegacyRenewalService _legacy;
        private readonly IRenewalService _current;
        private readonly ILogService _log;
        private readonly IInputService _input;

        public Importer(ILogService log, IInputService input, ILegacyRenewalService legacy, IRenewalService current)
        {
            _legacy = legacy;
            _current = current;
            _log = log;
            _input = input;
        }

        public void Import()
        {
            _log.Information("Legacy renewals {x}", _legacy.Renewals.Count().ToString());
            _log.Information("Current renewals {x}", _current.Renewals.Count().ToString());
            foreach (LegacyScheduledRenewal legacyRenewal in _legacy.Renewals)
            {
                var converted = Convert(legacyRenewal);
                _current.Import(converted);
            }
        }

        public ScheduledRenewal Convert(LegacyScheduledRenewal legacy)
        {
            var ret = new ScheduledRenewal
            {
                Target = Convert(legacy.Binding),
                Date = legacy.Date,
                New = true
            };

            ConvertValidation(legacy, ret);
            ConvertStore(legacy, ret);
            ConvertInstallation(legacy, ret);
            return ret;
        }

        public void ConvertValidation(LegacyScheduledRenewal legacy, ScheduledRenewal ret)
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
                        Credential = legacy.Binding.HttpFtpOptions
                    };
                    break;
                case "http-01.sftp":
                    ret.ValidationPluginOptions = new http.SftpOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Warmup = legacy.Warmup,
                        Credential = legacy.Binding.HttpFtpOptions
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

        public void ConvertStore(LegacyScheduledRenewal legacy, ScheduledRenewal ret)
        {           
            // Configure store
            if (!string.IsNullOrEmpty(legacy.CentralSslStore))
            {
                ret.StorePluginOptions = new CentralSslOptions()
                {
                    Path = legacy.CentralSslStore,
                    KeepExisting = legacy.KeepExisting == true
                };
            }
            else
            {
                ret.StorePluginOptions = new CertificateStorePluginOptions()
                {
                    StoreName = legacy.CertificateStore,
                    KeepExisting = legacy.KeepExisting == true
                };
            }
        }

        public void ConvertInstallation(LegacyScheduledRenewal legacy, ScheduledRenewal ret)
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

        public Target Convert(LegacyTarget legacy)
        {
            var ret = new Target
            {
                AlternativeNames = legacy.AlternativeNames,
                CommonName = legacy.CommonName,
                ExcludeBindings = legacy.ExcludeBindings,
                Host = legacy.Host,
                HostIsDns = legacy.HostIsDns == true,
                TargetPluginName = legacy.TargetPluginName,
                TargetSiteId = legacy.TargetSiteId
            };
            return ret;
        }
    }
}
