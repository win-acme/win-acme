using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using TransIp.Library.Dto;

namespace TransIp.Library
{
    public class DnsService : BaseServiceAuthenticated
    {
        public DnsService(AuthenticationService authenticationService, IProxyService proxyService) : 
            base(authenticationService, proxyService) { }
       
        public async Task<IEnumerable<Domain>?> ListDomains()
        {
            var response = await Get<DomainList>($"domains");
            return response.PayloadTyped?.Domains;
        }

        public async Task<IEnumerable<DnsEntry>?> ListDnsEntries(string domainName)
        {
            var response = await Get<DnsEntryList>($"domains/{domainName}/dns");
            return response.PayloadTyped?.DnsEntries;
        }

        public async Task CreateDnsEntry(string domainName, DnsEntry entry) => 
            _ = await Post($"domains/{domainName}/dns", new DnsEntryWrapper() { DnsEntry = entry });

        public async Task UpdateDnsEntry(string domainName, DnsEntry entry) => 
            _ = await Patch($"domains/{domainName}/dns", new DnsEntryWrapper() { DnsEntry = entry });

        public async Task DeleteDnsEntry(string domainName, DnsEntry entry) => 
            _ = await Delete($"domains/{domainName}/dns", new DnsEntryWrapper() { DnsEntry = entry });
    }
}
