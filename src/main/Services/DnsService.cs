using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Nager.PublicSuffix;
using PKISharp.WACS.Services.Interfaces;
using Serilog.Context;

namespace PKISharp.WACS.Services
{
	public class DnsService : IDnsService
	{
		private readonly DomainParser _domainParser;
		private readonly ILogService _log;

		public DnsService(DomainParser domainParser, ILogService logService)
		{
			_domainParser = domainParser;
			_log = logService;
		}

		public string GetRootDomain(string domainName)
		{
			if (domainName.EndsWith("."))
			{
				domainName = domainName.TrimEnd('.');
			}

			return _domainParser.Get(domainName).RegistrableDomain;
		}

		public IEnumerable<IPAddress> GetNameServerIpAddresses(ILookupClient lookupClient, string domainName)
		{
			var rootDomain = GetRootDomain(domainName);

            using (LogContext.PushProperty("Domain", rootDomain))
			{
				_log.Debug("Querying name servers");
				var nsResponse = lookupClient.Query(rootDomain, QueryType.NS);

				return GetNameServerIpAddresses(lookupClient, nsResponse.Answers.NsRecords());
			}
		}

		public IEnumerable<IPAddress> GetNameServerIpAddresses(ILookupClient lookupClient, IEnumerable<NsRecord> nsRecords)
		{
			foreach (var nsRecord in nsRecords)
			{
				using (LogContext.PushProperty("NameServer", nsRecord.NSDName))
				{
					_log.Debug("Querying IP for name server");
					var aResponse = lookupClient.Query(nsRecord.NSDName, QueryType.A);

					var nameServerIp = aResponse.Answers.ARecords().FirstOrDefault()?.Address;
					_log.Debug("Name server IP {NameServerIpAddress} identified", nameServerIp);

					yield return nameServerIp;
				}
			}
		}
	}
}