using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [IPlugin.Plugin<
        DreamhostOptions, DreamhostOptionsFactory,
        DnsValidationCapability, DreamhostJson>
        ("2bfb3ef8-64b8-47f1-8185-ea427b793c1a", 
        "Dreamhost", "Create verification records in Dreamhost DNS",
        ChallengeType = Constants.Dns01ChallengeType)]
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
