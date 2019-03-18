using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using Nager.PublicSuffix;
using PKISharp.WACS.Services;
using Serilog.Context;

namespace PKISharp.WACS.Clients.DNS
{
    public class LookupClientWrapper
    {
        private readonly ILogService _log;
        private readonly DomainParser _domainParser;
        public ILookupClient LookupClient { get; private set; }

        public LookupClientWrapper(DomainParser domainParser, ILogService logService, ILookupClient lookupClient)
        {
            LookupClient = lookupClient;
            _log = logService;
            _domainParser = domainParser;
        }

        public string GetRootDomain(string domainName)
        {
            if (domainName.EndsWith("."))
            {
                domainName = domainName.TrimEnd('.');
            }
            return _domainParser.Get(domainName).RegistrableDomain;
        }

        public IEnumerable<IPAddress> GetNameServerIpAddresses(string domainName)
        {
            var rootDomain = GetRootDomain(domainName);

            using (LogContext.PushProperty("Domain", rootDomain))
            {
                _log.Debug("Querying name servers");
                var nsResponse = LookupClient.Query(rootDomain, QueryType.NS);
                return GetNameServerIpAddresses(nsResponse.Answers.NsRecords());
            }
        }

        public IEnumerable<IPAddress> GetNameServerIpAddresses(IEnumerable<NsRecord> nsRecords)
        {
            foreach (var nsRecord in nsRecords)
            {
                using (LogContext.PushProperty("NameServer", nsRecord.NSDName))
                {
                    _log.Debug("Querying IP for name server");
                    var aResponse = LookupClient.Query(nsRecord.NSDName, QueryType.A);
                    var nameServerIp = aResponse.Answers.ARecords().FirstOrDefault()?.Address;
                    _log.Debug("Name server IP {NameServerIpAddress} identified", nameServerIp);
                    yield return nameServerIp;
                }
            }
        }

        public IEnumerable<string> GetTextRecordValues(string challengeUri)
        {
            IDnsQueryResponse result = LookupClient.Query(challengeUri, QueryType.TXT);
            result = RecursivelyFollowCnames(result);

            return result.Answers.TxtRecords().
                Select(txtRecord => txtRecord?.EscapedText?.FirstOrDefault()).
                Where(txtRecord => txtRecord != null).
                ToList();
        }

        private IDnsQueryResponse RecursivelyFollowCnames(IDnsQueryResponse result)
        {
            if (result.Answers.CnameRecords().Any())
            {
                var cname = result.Answers.CnameRecords().First();
                IDnsQueryResponse nsResponse = LookupClient.Query(cname.CanonicalName, QueryType.NS);
                IEnumerable<NsRecord> nsRecords = nsResponse.Answers.NsRecords().Union(nsResponse.Authorities.NsRecords()).ToList();
                LookupClientWrapper recursiveClient = this;
                if (nsRecords.Any())
                {
                    IEnumerable<IPAddress> nameServerIpAddresses = GetNameServerIpAddresses(nsRecords);
                    recursiveClient = new LookupClientWrapper(_domainParser, _log, new LookupClient(nameServerIpAddresses.ToArray()));
                }
                IDnsQueryResponse txtResponse = recursiveClient.LookupClient.Query(cname.CanonicalName, QueryType.TXT);
                _log.Debug("Name server {NameServerIpAddress} selected", txtResponse.NameServer.Endpoint.Address.ToString());
                return recursiveClient.RecursivelyFollowCnames(txtResponse);
            }

            return result;
        }
    }
}