using Nager.PublicSuffix;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns;
using PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class DreamhostDnsValidation : DnsValidation<DreamhostOptions, DreamhostDnsValidation>
    {
        private readonly DnsManagementClient _client;

        public DreamhostDnsValidation(DomainParser domainParser, ILookupClientProvider lookupClientProvider, ILogService logService, DreamhostOptions options, string identifier) : base(domainParser, lookupClientProvider, logService, options, identifier)
        {
            _client = new DnsManagementClient(options.ApiKey, logService);
        }

        public override void CreateRecord(string recordName, string token)
        {
            _client.CreateRecord(recordName, RecordType.TXT, token);
        }

        public override void DeleteRecord(string recordName, string token)
        {
            _client.DeleteRecord(recordName, RecordType.TXT, token);
        }
    }
}
