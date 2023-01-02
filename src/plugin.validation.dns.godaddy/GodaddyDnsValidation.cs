using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Godaddy;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [IPlugin.Plugin<GodaddyOptions, GodaddyOptionsFactory, GodaddyJson>
        ("966c4c3d-1572-44c7-9134-5e2bc8fa021d", 
        "Godaddy", 
        "Create verification records in Godaddy DNS",
        ChallengeType = Constants.Dns01ChallengeType)]
    internal class GodaddyDnsValidation : DnsValidation<GodaddyDnsValidation>
    {
        private readonly DnsManagementClient _client;
        private readonly DomainParseService _domainParser;

        public GodaddyDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            DomainParseService domainParser,
            GodaddyOptions options,
            SecretServiceManager ssm,
            IProxyService proxyService)
            : base(dnsClient, logService, settings)
        {
            _client = new DnsManagementClient(
                ssm.EvaluateSecret(options.ApiKey) ?? "", 
                ssm.EvaluateSecret(options.ApiSecret) ?? "", 
                logService, proxyService);
            _domainParser = domainParser;
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var domain = _domainParser.GetRegisterableDomain(record.Authority.Domain);
                var recordName = RelativeRecordName(domain, record.Authority.Domain);
                await _client.CreateRecord(domain, recordName, RecordType.TXT, record.Value);
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
                var domain = _domainParser.GetRegisterableDomain(record.Authority.Domain);
                var recordName = RelativeRecordName(domain, record.Authority.Domain);
                await _client.DeleteRecord(domain, recordName, RecordType.TXT);
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Godaddy: {ex.Message}");
            }
        }
    }
}
