using System.Collections.Generic;
using System.Net;
using DnsClient;
using DnsClient.Protocol;

namespace PKISharp.WACS.Services.Interfaces
{
	public interface IDnsService
	{
		string GetRootDomain(string domainName);
		IEnumerable<IPAddress> GetNameServerIpAddresses(ILookupClient lookupClient, string domainName);
		IEnumerable<IPAddress> GetNameServerIpAddresses(ILookupClient lookupClient, IEnumerable<NsRecord> nsRecords);
	}
}