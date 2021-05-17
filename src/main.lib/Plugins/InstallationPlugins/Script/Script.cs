using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

        public async Task<bool> Install(Target target, IEnumerable<IStorePlugin> store, CertificateInfo newCertificate, CertificateInfo? oldCertificate)
        {
            if (_options.Script != null)
            {
                var defaultStoreType = store.First().GetType();
                var defaultStoreInfo = newCertificate.StoreInfo[defaultStoreType];
                var parameters = ReplaceParameters(_options.ScriptParameters ?? "", defaultStoreInfo, newCertificate, oldCertificate, false);
                var censoredParameters = ReplaceParameters(_options.ScriptParameters ?? "", defaultStoreInfo, newCertificate, oldCertificate, true);
                return await _client.RunScript(_options.Script, parameters, censoredParameters);
            }
            return false;
        }

        private string ReplaceParameters(string input, StoreInfo defaultStoreInfo, CertificateInfo newCertificate, CertificateInfo? oldCertificate, bool censor)
        {
            // Numbered parameters for backwards compatibility only,
            // do not extend for future updates
            return Regex.Replace(input, "{.+?}", (m) => {
                return m.Value switch
                {
                    "{0}" or "{CertCommonName}" => newCertificate.CommonName.Value,
                    "{1}" or "{CachePassword}" => (censor ? _renewal.PfxPassword?.Value : _renewal.PfxPassword?.DisplayValue) ?? "",
                    "{2}" or "{CacheFile}" => newCertificate.CacheFile?.FullName ?? "",
                    "{3}" or "{StorePath}" => defaultStoreInfo.Path ?? "",
                    "{4}" or "{CertFriendlyName}" => newCertificate.Certificate.FriendlyName,
                    "{5}" or "{CertThumbprint}" => newCertificate.Certificate.Thumbprint,
                    "{6}" or "{CacheFolder}" => newCertificate.CacheFile?.Directory?.FullName ?? "",
                    "{7}" or "{RenewalId}" => _renewal.Id,
                    "{StoreType}" => defaultStoreInfo.Name ?? "",
                    "{OldCertCommonName}" => oldCertificate?.CommonName?.Value ?? "",
                    "{OldCertFriendlyName}" => oldCertificate?.Certificate.FriendlyName ?? "",
                    "{OldCertThumbprint}" => oldCertificate?.Certificate.Thumbprint ?? "",
                    _ => m.Value
                };
            });
        }

        (bool, string?) IPlugin.Disabled => (false, null);
    }
}