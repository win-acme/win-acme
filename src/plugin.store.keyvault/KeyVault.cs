using Azure.Security.KeyVault.Certificates;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Handle creation of DNS records in Azure
    /// </summary>
    [IPlugin.Plugin<
        KeyVaultOptions, KeyVaultOptionsFactory, 
        DefaultCapability, KeyVaultJson>
        ("dbfa91e2-28c0-4b37-857c-df6575dbb388", 
        "KeyVault", 
        "Store certificate in Azure Key Vault")]
    internal class KeyVault : IStorePlugin
    {
        private readonly KeyVaultOptions _options;
        private readonly ILogService _log;
        private readonly AzureHelpers _helpers;

        public KeyVault(KeyVaultOptions options, SecretServiceManager ssm, IProxyService proxyService, ILogService log)
        {
            _options = options;
            _log = log;
            _helpers = new AzureHelpers(options, proxyService, ssm);
        }

        public Task Delete(CertificateInfo certificateInfo) => Task.CompletedTask;
        public async Task Save(CertificateInfo certificateInfo)
        {
            var client = new CertificateClient(
                new Uri($"https://{_options.VaultName}.vault.azure.net/"),
                _helpers.TokenCredential,
                new CertificateClientOptions() {
                    Transport = _helpers.ArmOptions.Transport
                });
            var importOptions = new ImportCertificateOptions(
                _options.CertificateName,
                certificateInfo.Collection.Export(X509ContentType.Pfx));
            try
            {
                _ = await client.ImportCertificateAsync(importOptions);
                certificateInfo.StoreInfo.Add(
                    GetType(),
                    new StoreInfo()
                    {
                        Path = _options.VaultName,
                        Name = _options.CertificateName
                    });
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error importing certificate to KeyVault");
            }

        }
    }
}