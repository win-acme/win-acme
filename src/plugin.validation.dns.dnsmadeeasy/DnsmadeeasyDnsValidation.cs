using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.DnsMadeEasy;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{

    [IPlugin.Plugin<
        DnsMadeEasyOptions, DnsMadeEasyOptionsFactory,
        DnsValidationCapability, DnsMadeEasyJson>
        ("13993334-2d74-4ff6-801b-833b99bf231d",
        "DnsMadeEasy", "Create verification records in DnsMadeEasy DNS")]
    internal class DnsMadeEasyDnsValidation : DnsValidation<DnsMadeEasyDnsValidation>
    {
        private readonly DnsManagementClient _client;
        private readonly DomainParseService _domainParser;

        public DnsMadeEasyDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            DomainParseService domainParser,
            DnsMadeEasyOptions options,
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
            catch (Exception ex)
            {
                _log.Warning($"Unable to create record at DnsMadeEasy: {ex.Message}");
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
                _log.Warning($"Unable to delete record from DnsMadeEasy: {ex.Message}");
            }
        }
    }
}
