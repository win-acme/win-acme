using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{

    internal class DreamhostDnsValidation : DnsValidation<DreamhostDnsValidation>
    {
        private readonly DnsManagementClient _client;

        public DreamhostDnsValidation(
            LookupClientProvider dnsClient, 
            ILogService logService, 
            ISettingsService settings,
            SecretServiceManager ssm,
            DreamhostOptions options)
            : base(dnsClient, logService, settings) 
            => _client = new DnsManagementClient(ssm.EvaluateSecret(options.ApiKey) ?? "", logService);

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                await _client.CreateRecord(record.Authority.Domain, RecordType.TXT, record.Value);
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
                await _client.DeleteRecord(record.Authority.Domain, RecordType.TXT, record.Value);
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Dreamhost: {ex.Message}");
            }
        }
    }
}
