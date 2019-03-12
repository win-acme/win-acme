using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using PKISharp.WACS.Services.Interfaces;

namespace PKISharp.WACS.Services
{
	public class AcmeDnsValidationClient
	{
		private readonly IDnsService _dnsService;
		private readonly ILogService _log;

		public AcmeDnsValidationClient(IDnsService dnsService, ILogService logService)
		{
			_dnsService = dnsService;
			_log = logService;
		}

		public IEnumerable<string> GetTextRecordValues(ILookupClient lookupClient, string challengeUri)
		{
			IDnsQueryResponse result = lookupClient.Query(challengeUri, QueryType.TXT);
			result = RecursivelyFollowCnames(lookupClient, result);

			return result.Answers.TxtRecords().Select(txtRecord => txtRecord?.EscapedText?.FirstOrDefault()).Where(txtRecord => txtRecord != null).ToList();
		}

		private IDnsQueryResponse RecursivelyFollowCnames(ILookupClient lookupClient, IDnsQueryResponse result)
		{
			if (result.Answers.CnameRecords().Any())
			{
				var cname = result.Answers.CnameRecords().First();
				IDnsQueryResponse nsResponse = lookupClient.Query(cname.CanonicalName, QueryType.NS);
				IEnumerable<NsRecord> nsRecords = nsResponse.Answers.NsRecords().Union(nsResponse.Authorities.NsRecords()).ToList();

				ILookupClient internalLookupClient = lookupClient;

				if (nsRecords.Any())
				{
					IEnumerable<IPAddress> nameServerIpAddresses = _dnsService.GetNameServerIpAddresses(internalLookupClient, nsRecords);
					internalLookupClient = new LookupClient(nameServerIpAddresses.ToArray());
				}

				IDnsQueryResponse txtResponse = internalLookupClient.Query(cname.CanonicalName, QueryType.TXT);
				_log.Debug("Name server {NameServerIpAddress} selected", txtResponse.NameServer.Endpoint.Address.ToString());

				return RecursivelyFollowCnames(internalLookupClient, txtResponse);
			}

			return result;
		}
	}
}