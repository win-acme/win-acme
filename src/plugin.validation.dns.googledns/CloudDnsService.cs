using Google.Apis.Dns.v1;
using Google.Apis.Dns.v1.Data;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class CloudDnsService
    {
        private DnsService _client;

        public CloudDnsService(DnsService client)
        {
            _client = client;
        }

        public async Task<IList<ManagedZone>> GetManagedZones(string projectId)
        {
            var request = _client.ManagedZones.List(projectId);
            var response = await request.ExecuteAsync();
            return response.ManagedZones.ToList();
        }

        public async Task<ManagedZone?> FindZone(string projectId, string dnsName)
        {
            var zones = await GetManagedZones(projectId);
            return zones.FirstOrDefault(z => z.DnsName.StartsWith(dnsName));
        }

        public async Task<ResourceRecordSet> CreateTxtRecord(string projectId, ManagedZone zone, string name, string value)
        {
            if (!name.EndsWith("."))
                name += ".";

            var body = new ResourceRecordSet();
            body.Kind = "dns#resourceRecordSet";
            body.Name = name;
            body.Type = "TXT";
            body.Ttl = 0;
            body.Rrdatas = new List<string>(){"\"" + value + "\"" };

            var request = _client.Projects.ManagedZones.Rrsets.Create(body, projectId, zone.Name);

            return await request.ExecuteAsync();
        }

        public async Task<ResourceRecordSetsDeleteResponse> DeleteTxtRecord(string projectId, ManagedZone zone, string name)
        {
            if (!name.EndsWith("."))
                name += ".";

            var request = _client.Projects.ManagedZones.Rrsets.Delete(projectId, zone.Name, name, "TXT");
            return await request.ExecuteAsync();
        }
    }
}
