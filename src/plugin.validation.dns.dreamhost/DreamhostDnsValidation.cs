using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class DreamhostDnsValidation : DnsValidation<DreamhostDnsValidation>
    {
        private readonly DnsManagementClient _client;

        public DreamhostDnsValidation(LookupClientProvider dnsClient, ILogService logService, DreamhostOptions options) : base(dnsClient, logService) => _client = new DnsManagementClient(options.ApiKey.Value, logService);

        public override Task CreateRecord(string recordName, string token) => _client.CreateRecord(recordName, RecordType.TXT, token);

        public override Task DeleteRecord(string recordName, string token) => _client.DeleteRecord(recordName, RecordType.TXT, token);
    }
}
