using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Dnsexit;
using PKISharp.WACS.Services;
using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [IPlugin.Plugin<
        DnsexitOptions, DnsexitOptionsFactory, 
        DnsValidationCapability, DnsexitJson>
        ("C9017182-1000-4257-A8DA-0553CD1490EC", 
        "Dnsexit", "Create verification records in Dnsexit DNS")]
    internal class DnsExitDnsValidation : DnsValidation<DnsExitDnsValidation>
    {
        private readonly DnsManagementClient _client;
        private readonly DomainParseService _domainParser;

        public DnsExitDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            DomainParseService domainParser,
            DnsexitOptions options,
            SecretServiceManager ssm,
            IProxyService proxyService)
            : base(dnsClient, logService, settings)
        {
            _client = new DnsManagementClient(
                ssm.EvaluateSecret(options.ApiKey) ?? "", 
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
                _log.Warning($"Unable to delete record from DnsExit: {ex.Message}");
            }
        }
    }
}
