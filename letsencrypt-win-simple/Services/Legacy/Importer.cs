using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.StorePlugins;
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
                KeepExisting = legacy.KeepExisting,
                New = true,
                Script = legacy.Script,
                ScriptParameters = legacy.ScriptParameters,
                Warmup = legacy.Warmup,
            };
            if (!string.IsNullOrEmpty(legacy.CentralSslStore))
            {
                ret.StorePluginOptions = new CentralSslStorePluginOptions()
                {
                    Path = legacy.CentralSslStore,
                    AllowOverwrite = legacy.KeepExisting != true
                };
            }
            else
            {
                ret.StorePluginOptions = new CertificateStorePluginOptions()
                {
                    StoreName = legacy.CertificateStore
                };
            }
            return ret;
        }

        public Target Convert(LegacyTarget legacy)
        {
            var ret = new Target
            {
                AlternativeNames = legacy.AlternativeNames,
                CommonName = legacy.CommonName,
                DnsAzureOptions = legacy.DnsAzureOptions,
                DnsScriptOptions = legacy.DnsScriptOptions,
                ExcludeBindings = legacy.ExcludeBindings,
                FtpSiteId = legacy.FtpSiteId,
                Host = legacy.Host,
                HostIsDns = legacy.HostIsDns == true,
                HttpFtpOptions = legacy.HttpFtpOptions,
                HttpWebDavOptions = legacy.HttpWebDavOptions,
                IIS = legacy.IIS == true,
                InstallationSiteId = legacy.InstallationSiteId,
                SSLIPAddress = legacy.SSLIPAddress,
                SSLPort = legacy.SSLPort,
                TargetPluginName = legacy.TargetPluginName,
                TargetSiteId = legacy.TargetSiteId,
                ValidationPluginName = legacy.ValidationPluginName,
                ValidationPort = legacy.ValidationPort,
                ValidationSiteId = legacy.ValidationSiteId,
                WebRootPath = legacy.WebRootPath
            };
            return ret;
        }
    }
}
