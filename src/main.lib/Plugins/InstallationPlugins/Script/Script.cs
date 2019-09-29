using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class Script : ScriptClient, IInstallationPlugin
    {
        private readonly Renewal _renewal;
        private readonly ScriptOptions _options;

        public Script(Renewal renewal, ScriptOptions options, ILogService logService) : base(logService)
        {
            _options = options;
            _renewal = renewal;
        }

        public async Task Install(IEnumerable<IStorePlugin> store, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            var defaultStoreType = store.First().GetType();
            var defaultStoreInfo = newCertificate.StoreInfo[defaultStoreType];
            var parameters = _options.ScriptParameters ?? "";
            parameters = parameters.Replace("{0}", newCertificate.SubjectName);
            parameters = parameters.Replace("{1}", _renewal.PfxPassword?.Value);
            parameters = parameters.Replace("{2}", newCertificate.CacheFile?.FullName);
            parameters = parameters.Replace("{3}", defaultStoreInfo.Path);
            parameters = parameters.Replace("{4}", newCertificate.Certificate.FriendlyName);
            parameters = parameters.Replace("{5}", newCertificate.Certificate.Thumbprint);
            parameters = parameters.Replace("{6}", newCertificate.CacheFile?.Directory.FullName);
            parameters = parameters.Replace("{7}", _renewal.Id);

            parameters = parameters.Replace("{CachePassword}", _renewal.PfxPassword?.Value);
            parameters = parameters.Replace("{CacheFile}", newCertificate.CacheFile?.FullName);
            parameters = parameters.Replace("{CacheFolder}", newCertificate.CacheFile?.FullName);
            parameters = parameters.Replace("{CertCommonName}", newCertificate.SubjectName);
            parameters = parameters.Replace("{CertFriendlyName}", newCertificate.Certificate.FriendlyName);
            parameters = parameters.Replace("{CertThumbprint}", newCertificate.Certificate.Thumbprint);
            parameters = parameters.Replace("{StoreType}", defaultStoreInfo.Name);
            parameters = parameters.Replace("{StorePath}", defaultStoreInfo.Path);
            parameters = parameters.Replace("{RenewalId}", _renewal.Id);
            await RunScript(_options.Script, parameters);
        }

        public bool Disabled => false;
    }
}