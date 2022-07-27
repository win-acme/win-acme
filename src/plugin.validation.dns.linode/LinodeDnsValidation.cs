using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins.Linode;
using PKISharp.WACS.Services;
using System.Runtime.Versioning;

[assembly: SupportedOSPlatform("windows")]

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class LinodeDnsValidation : DnsValidation<LinodeDnsValidation>
    {
        private readonly DnsManagementClient _client;
        private readonly DomainParseService _domainParser;
        private new readonly ILogService _log;
        private int _linodeDomainId;
        private int _linodeRecordId;

        public LinodeDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,
            ISettingsService settings,
            DomainParseService domainParser,
            LinodeOptions options,
            SecretServiceManager ssm,
            IProxyService proxyService)
            : base(dnsClient, logService, settings)
        {
            _client = new DnsManagementClient(
                ssm.EvaluateSecret(options.ApiToken) ?? "",
                logService, proxyService);
            _domainParser = domainParser;
            _log = logService;
        }

        public override async Task<bool> CreateRecord(DnsValidationRecord record)
        {
            try
            {
                var domain = _domainParser.GetRegisterableDomain(record.Authority.Domain);
                var recordName = RelativeRecordName(domain, record.Authority.Domain);
                _linodeDomainId = await _client.GetDomainId(domain);

                if(_linodeDomainId == 0)
                {
                    throw new InvalidDataException("Linode did not return a valid domain id.");
                }

                _linodeRecordId = await _client.CreateRecord(_linodeDomainId, recordName, record.Value);

                if (_linodeRecordId == 0)
                {
                    throw new InvalidDataException("Linode did not return a valid domain record id.");
                }

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
                await _client.DeleteRecord(_linodeDomainId, _linodeRecordId, recordName);
            }
            catch (Exception ex)
            {
                _log.Warning($"Unable to delete record from Linode: {ex.Message}");
            }
        }
    }
}
