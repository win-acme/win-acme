using System.Collections.Generic;

namespace PKISharp.WACS.Clients.DNS
{
    public class DnsLookupResult
    {
        public DnsLookupResult(string domain, IEnumerable<LookupClientWrapper> nameServers, DnsLookupResult? cnameFrom = null)
        {
            Nameservers = nameServers;
            Domain = domain;
            From = cnameFrom;
        }

        public IEnumerable<LookupClientWrapper> Nameservers { get; set; }
        public string Domain { get; set; }
        public DnsLookupResult? From { get; set; }
    }
}
