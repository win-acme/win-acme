using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using System.Linq;

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
                InstallationPluginNames = legacy.InstallationPluginNames,
                New = true,
                Script = legacy.Script,
                ScriptParameters = legacy.ScriptParameters
            };

            ConvertValidation(legacy, ret);
            ConvertStore(legacy, ret);
            return ret;
        }

        public void ConvertValidation(LegacyScheduledRenewal legacy, ScheduledRenewal ret)
        {
            // Configure validation
            switch (legacy.Binding.ValidationPluginName)
            {
                case "dns-01.Script":
                case "dns-01.DnsScript":
                    ret.ValidationPluginOptions = new ScriptOptions()
                    {
                        ScriptConfiguration = legacy.Binding.DnsScriptOptions
                    };
                    break;
                case "dns-01.Azure":
                    ret.ValidationPluginOptions = new AzureOptions()
                    {
                        AzureConfiguration = legacy.Binding.DnsAzureOptions
                    };
                    break;
                case "http-01.Ftp":
                    ret.ValidationPluginOptions = new FtpOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Warmup = legacy.Warmup,
                        Credential = legacy.Binding.HttpFtpOptions
                    };
                    break;
                case "http-01.Sftp":
                    ret.ValidationPluginOptions = new SftpOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        Warmup = legacy.Warmup,
                        Credential = legacy.Binding.HttpFtpOptions
                    };
                    break;
                case "http-01.IIS":
                case "http-01.SelfHosting":
                    ret.ValidationPluginOptions = new SelfHostingOptions()
                    {
                        Port = legacy.Binding.ValidationPort
                    };
                    break;
                case "http-01.FileSystem":
                default:
                    ret.ValidationPluginOptions = new FileSystemOptions()
                    {
                        CopyWebConfig = legacy.Binding.IIS == true,
                        Path = legacy.Binding.WebRootPath,
                        ValidationSiteId = legacy.Binding.ValidationSiteId,
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

        public Target Convert(LegacyTarget legacy)
        {
            var ret = new Target
            {
                AlternativeNames = legacy.AlternativeNames,
                CommonName = legacy.CommonName,
                ExcludeBindings = legacy.ExcludeBindings,
                FtpSiteId = legacy.FtpSiteId,
                Host = legacy.Host,
                HostIsDns = legacy.HostIsDns == true,
                InstallationSiteId = legacy.InstallationSiteId,
                SSLIPAddress = legacy.SSLIPAddress,
                SSLPort = legacy.SSLPort,
                TargetPluginName = legacy.TargetPluginName,
                TargetSiteId = legacy.TargetSiteId
            };
            return ret;
        }
    }
}
