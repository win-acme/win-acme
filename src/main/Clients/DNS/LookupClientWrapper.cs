using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsClient;
using DnsClient.Protocol;
using PKISharp.WACS.Services;
using Serilog.Context;

namespace PKISharp.WACS.Clients.DNS
{
    public class LookupClientWrapper
    {
        private readonly ILogService _log;
        private readonly DomainParseService _domainParser;
        private readonly LookupClientProvider _provider;
        public ILookupClient LookupClient { get; private set; }

        public LookupClientWrapper(DomainParseService domainParser, ILogService logService, ILookupClient lookupClient, LookupClientProvider provider)
        {
            LookupClient = lookupClient;
            lookupClient.UseCache = false;
            _log = logService;
            _domainParser = domainParser;
            _provider = provider;
        }

        public string GetRootDomain(string domainName)
        {
            return _domainParser.GetRegisterableDomain(domainName.TrimEnd('.'));
        }

        public IEnumerable<IPAddress> GetAuthoritativeNameServers(string domainName)
        {
            domainName = domainName.TrimEnd('.');
            _log.Debug("Querying name servers for {part}", domainName);
            var nsResponse = LookupClient.Query(domainName, QueryType.NS);
            var nsRecords = nsResponse.Answers.NsRecords();
            if (nsRecords.Any())
            {
                return GetNameServerIpAddresses(nsRecords);
            }
            return null;
        }

        private IEnumerable<IPAddress> GetNameServerIpAddresses(IEnumerable<NsRecord> nsRecords)
        {
            foreach (var nsRecord in nsRecords)
            {
                using (LogContext.PushProperty("NameServer", nsRecord.NSDName))
                {
                    _log.Debug("Querying IP for name server");
                     var aResponse = _provider.DefaultClient.LookupClient.Query(nsRecord.NSDName, QueryType.A);
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
                var recursiveClient = _provider.GetClient(cname.CanonicalName);
                IDnsQueryResponse txtResponse = recursiveClient.LookupClient.Query(cname.CanonicalName, QueryType.TXT);
                _log.Debug("Name server {NameServerIpAddress} selected", txtResponse.NameServer.Endpoint.Address.ToString());
                return recursiveClient.RecursivelyFollowCnames(txtResponse);
            }
            return result;
        }
    }
}