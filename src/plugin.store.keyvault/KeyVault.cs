using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Azure.Common;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    /// <summary>
    /// Handle creation of DNS records in Azure
    /// </summary>
    internal class KeyVault : IStorePlugin
    {
        private CertificateClient? _azureKeyVaultClient;
        private readonly IProxyService _proxyService;
        private readonly KeyVaultOptions _options;
        private readonly ILogService _log;

        public KeyVault(KeyVaultOptions options, IProxyService proxyService, ILogService log)
        {
            _options = options;
            _proxyService = proxyService;
            _log = log;
        }

        private Task<CertificateClient> GetClient()
        {
            if (_azureKeyVaultClient == null)
            {
                var credential = _options.UseMsi
                    ? new ManagedIdentityCredential()
                    : (TokenCredential)new ClientSecretCredential(
                        _options.TenantId,
                        _options.ClientId,
                        _options.Secret?.Value);
                var options = new CertificateClientOptions
                {
                    Transport = new HttpClientTransport(_proxyService.GetHttpClient())
                };
                var client = new CertificateClient(new Uri($"https://{_options.VaultName}.vault.azure.net/"), credential, options);
                _azureKeyVaultClient = client;
            }
            return Task.FromResult(_azureKeyVaultClient);
        }

        public (bool, string) Disabled => (false, "");
        public Task Delete(CertificateInfo certificateInfo) => Task.CompletedTask;
        public async Task Save(CertificateInfo certificateInfo)
        {
            var client = await GetClient();
            var importOptions = new ImportCertificateOptions(
                _options.CertificateName,
                await File.ReadAllBytesAsync(certificateInfo.CacheFile!.FullName));
            importOptions.Password = certificateInfo.CacheFilePassword;
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
