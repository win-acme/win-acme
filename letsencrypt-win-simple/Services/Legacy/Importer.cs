using PKISharp.WACS.DomainObjects;
using dl = PKISharp.WACS.DomainObjects.Legacy;
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
            foreach (dl.ScheduledRenewal legacyRenewal in _legacy.Renewals)
            {
                var converted = Convert(legacyRenewal);
                _current.Import(converted);
            }
        }

        public ScheduledRenewal Convert(dl.ScheduledRenewal legacy)
        {
            var ret = new ScheduledRenewal
            {
                Binding = Convert(legacy.Binding),
                CentralSslStore = legacy.CentralSslStore,
                CertificateStore = legacy.CertificateStore,
                Date = legacy.Date,
                InstallationPluginNames = legacy.InstallationPluginNames,
                KeepExisting = legacy.KeepExisting,
                New = true,
                Script = legacy.Script,
                ScriptParameters = legacy.ScriptParameters,
                Warmup = legacy.Warmup,
            };
            return ret;
        }

        public Target Convert(dl.Target legacy)
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
                HostIsDns = legacy.HostIsDns,
                HttpFtpOptions = legacy.HttpFtpOptions,
                HttpWebDavOptions = legacy.HttpWebDavOptions,
                IIS = legacy.IIS,
                InstallationSiteId = legacy.InstallationSiteId,
                PluginName = legacy.PluginName,
                SiteId = legacy.SiteId,
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
