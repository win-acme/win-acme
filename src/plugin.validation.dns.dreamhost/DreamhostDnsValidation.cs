using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins
{
    internal class DreamhostDnsValidation : DnsValidation<DreamhostOptions, DreamhostDnsValidation>
    {
        private readonly DnsManagementClient _client;

        public DreamhostDnsValidation(
            LookupClientProvider dnsClient,
            ILogService logService,  
            DreamhostOptions options,  
            string identifier) : 
            base(dnsClient, logService, options, identifier)
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
