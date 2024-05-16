using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Linode;
using PKISharp.WACS.Services;
using System.Collections.Concurrent;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    [IPlugin.Plugin<
        LinodeOptions, LinodeOptionsFactory,
        DnsValidationCapability, LinodeJson>
        ("12fdc54c-be30-4458-8066-2c1c565fe2d9",
        "Linode", "Create verification records in Linode DNS")]
    internal class LinodeDnsValidation : DnsValidation<LinodeDnsValidation>
    {
        private readonly DnsManagementClient _client;
        private readonly DomainParseService _domainParser;
        private readonly Dictionary<string, int> _domainIds = new();
        private readonly ConcurrentDictionary<int, List<int>> _recordIds = new();
        private new readonly ILogService _log;

        public LinodeDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            DomainParseService domainParser,
            LinodeOptions options,
            SecretServiceManager ssm,
            IProxyService proxyService) : base(dnsClient, logService, settings)
        {
            _client = new DnsManagementClient(ssm.EvaluateSecret(options.ApiToken) ?? "", logService, proxyService);
            _domainParser = domainParser;
            _log = logService;
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var domain = _domainParser.GetRegisterableDomain(record.Authority.Domain);
                var domainId = await _client.GetDomainId(domain);
                if (domainId == 0)
                {
                    throw new InvalidDataException("Linode did not return a valid domain id.");
                }
                _ = _domainIds.TryAdd(record.Authority.Domain, domainId);

                var recordName = RelativeRecordName(domain, record.Authority.Domain);
                var recordId = await _client.CreateRecord(domainId, recordName, record.Value);
                if (recordId == 0)
                {
                    throw new InvalidDataException("Linode did not return a valid domain record id.");
                }
                _ = _recordIds.AddOrUpdate(
                    domainId,
                    new List<int> { recordId }, 
                    (b, s) => s.Append(recordId).ToList());
                return true;
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to create record at Linode: {ex.Message}");
                return false;
            }
        }

        public override async Task DeleteRecord(DnsValidationRecord record)
        {
            if (_domainIds.TryGetValue(record.Authority.Domain, out var domainId))
            {
                if (_recordIds.TryGetValue(domainId, out var recordIds))
                {
                    foreach (var recordId in recordIds)
                    {
                        try
                        {
                            _ = await _client.DeleteRecord(domainId, recordId);
                        }
                        catch (Exception ex)
                        {
                            _log.Warning("Unable to delete record {recordId} from Linode domain {domainId}: {message}", recordId, domainId, ex.Message);
                        }
                    }
                }
            }
        }

    }
}
