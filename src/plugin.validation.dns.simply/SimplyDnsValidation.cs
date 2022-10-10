using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins.Simply;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class SimplyDnsValidation : DnsValidation<SimplyDnsValidation>
    {
        private readonly SimplyDnsClient _client;

        public SimplyDnsValidation(
            LookupClientProvider dnsClient, 
            ILogService logService, 
            ISettingsService settings,
            IProxyService proxyService,
            SecretServiceManager ssm,
            SimplyOptions options)
            : base(dnsClient, logService, settings) 
            => _client = new SimplyDnsClient(
                options.Account ?? "",
                ssm.EvaluateSecret(options.ApiKey) ?? "",
                proxyService.GetHttpClient());

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var recordName = record.Authority.Domain;
                var product = await GetProductAsync(recordName);
                if (product.Object != null)
                {
                    await _client.CreateRecordAsync(product.Object, recordName, record.Value);
                    return true;
                }
            } 
            catch
            {  
            }
            return false;
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                var recordName = record.Authority.Domain;
                var product = await GetProductAsync(recordName);
                if (product.Object != null)
                {
                    await _client.DeleteRecordAsync(product.Object, record.Authority.Domain, record.Value);
                }
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Simply: {ex.Message}");
            }
        }

        private async Task<Product> GetProductAsync(string recordName)
        {
            var products = await _client.GetAllProducts();
            var product = FindBestMatch(products.ToDictionary(x => x.Domain?.NameIdn ?? "unknown", x => x), recordName);
            if (product is null)
            {
                throw new Exception($"Unable to find product for record '{recordName}'");
            }
            return product;
        }
    }
}
