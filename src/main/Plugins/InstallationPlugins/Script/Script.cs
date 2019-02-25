using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class Script : ScriptClient, IInstallationPlugin
    {
        private Renewal _renewal;
        private ScriptOptions _options;

        public Script(Renewal renewal, ScriptOptions options, ILogService logService) : base(logService)
        {
            _options = options;
            _renewal = renewal;
        }

        public void Install(IStorePlugin store, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            var parameters = _options.ScriptParameters ?? "";
            parameters = parameters.Replace("{0}", newCertificate.SubjectName);
            parameters = parameters.Replace("{1}", _renewal.PfxPassword);
            parameters = parameters.Replace("{2}", newCertificate.CacheFile?.FullName);
            parameters = parameters.Replace("{3}", newCertificate.StorePath);
            parameters = parameters.Replace("{4}", newCertificate.Certificate.FriendlyName);
            parameters = parameters.Replace("{5}", newCertificate.Certificate.Thumbprint);
            parameters = parameters.Replace("{6}", newCertificate.CacheFile?.Directory.FullName);
            parameters = parameters.Replace("{7}", _renewal.Id);

            parameters = parameters.Replace("{CachePassword}", _renewal.PfxPassword);
            parameters = parameters.Replace("{CacheFile}", newCertificate.CacheFile?.FullName);
            parameters = parameters.Replace("{CacheFolder}", newCertificate.CacheFile?.FullName);
            parameters = parameters.Replace("{CertCommonName}", newCertificate.SubjectName);
            parameters = parameters.Replace("{CertFriendlyName}", newCertificate.Certificate.FriendlyName);
            parameters = parameters.Replace("{CertThumbprint}", newCertificate.Certificate.Thumbprint);
            parameters = parameters.Replace("{StoreType}", _renewal.StorePluginOptions?.Name);
            parameters = parameters.Replace("{StorePath}", newCertificate.StorePath);
            parameters = parameters.Replace("{RenewalId}", _renewal.Id);
            RunScript(_options.Script, parameters);
        }
    }
}