using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.InstallationPlugins
{
    internal class Script : IInstallationPlugin
    {
        private readonly Renewal _renewal;
        private readonly ScriptOptions _options;
        private readonly ScriptClient _client;

        public Script(Renewal renewal, ScriptOptions options, ScriptClient client)
        {
            _options = options;
            _renewal = renewal;
            _client = client;
        }

        public async Task Install(IEnumerable<IStorePlugin> store, CertificateInfo newCertificate, CertificateInfo? oldCertificate)
        {
            if (_options.Script != null)
            {
                var defaultStoreType = store.First().GetType();
                var defaultStoreInfo = newCertificate.StoreInfo[defaultStoreType];
                var parameters = _options.ScriptParameters ?? "";
                
                // Numbered parameters for backwards compatibility only,
                // do not extend for future updates
                parameters = parameters.Replace("{0}", newCertificate.CommonName);
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
                parameters = parameters.Replace("{CertCommonName}", newCertificate.CommonName);
                parameters = parameters.Replace("{CertFriendlyName}", newCertificate.Certificate.FriendlyName);
                parameters = parameters.Replace("{CertThumbprint}", newCertificate.Certificate.Thumbprint);
                parameters = parameters.Replace("{StoreType}", defaultStoreInfo.Name);
                parameters = parameters.Replace("{StorePath}", defaultStoreInfo.Path);
                parameters = parameters.Replace("{RenewalId}", _renewal.Id);
                parameters = parameters.Replace("{OldCertCommonName}", oldCertificate?.CommonName);
                parameters = parameters.Replace("{OldCertFriendlyName}", oldCertificate?.Certificate.FriendlyName);
                parameters = parameters.Replace("{OldCertThumbprint}", oldCertificate?.Certificate.Thumbprint);

                await _client.RunScript(_options.Script, parameters);
            }
        }

        (bool, string?) IPlugin.Disabled => (false, null);
    }
}