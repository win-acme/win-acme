using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Context;
using PKISharp.WACS.Plugins.ValidationPlugins.Godaddy;
using PKISharp.WACS.Services;
using System;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class GodaddyDnsValidation : DnsValidation<GodaddyDnsValidation>
    {
        private readonly DnsManagementClient _client;

        public GodaddyDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            GodaddyOptions options,
            ProxyService proxyService)
            : base(dnsClient, logService, settings)
            => _client = new DnsManagementClient(options.ApiKey.Value, logService, proxyService);

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                await _client.CreateRecord(record.Authority.Domain, record.Context.Identifier, RecordType.TXT, record.Value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            try
            {
                await _client.DeleteRecord(record.Authority.Domain, record.Context.Identifier, RecordType.TXT, record.Value);
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Godaddy: {ex.Message}");
            }
        }
    }
}
