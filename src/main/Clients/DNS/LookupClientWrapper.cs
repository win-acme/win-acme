using System;
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
        private readonly LookupClientWrapper _default;
        public ILookupClient LookupClient { get; private set; }

        public LookupClientWrapper(DomainParser domainParser, ILogService logService, ILookupClient lookupClient, LookupClientWrapper @default)
        {
            LookupClient = lookupClient;
            lookupClient.UseCache = false;
            _log = logService;
            _domainParser = domainParser;
            _default = @default;
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
            var part = domainName.TrimEnd('.');
            do
            {
                using (LogContext.PushProperty("Domain", part))
                {
                    _log.Debug("Querying name servers for {part}", part);
                    var nsResponse = LookupClient.Query(part, QueryType.NS);
                    if (nsResponse.Answers.NsRecords().Any())
                    {
                        return GetNameServerIpAddresses(nsResponse.Answers.NsRecords());
                    }
                }
                part = part.Substring(part.IndexOf('.') + 1);
            }
            while (part.Length > rootDomain.Length);
            throw new Exception($"Unable to determine name servers for domain {domainName}");
        }

        public IEnumerable<IPAddress> GetNameServerIpAddresses(IEnumerable<NsRecord> nsRecords)
        {
            foreach (var nsRecord in nsRecords)
            {
                using (LogContext.PushProperty("NameServer", nsRecord.NSDName))
                {
                    _log.Debug("Querying IP for name server");
                    var nsChecker = this;
                    if (_default != null)
                    {
                        nsChecker = _default;
                    }
                    var aResponse = nsChecker.LookupClient.Query(nsRecord.NSDName, QueryType.A);
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
                var nsChecker = this;
                if (_default != null)
                {
                    nsChecker = _default;
                }
                var nameServerIpAddresses = nsChecker.GetNameServerIpAddresses(cname.CanonicalName);
                var recursiveClient = new LookupClientWrapper(_domainParser, _log, new LookupClient(nameServerIpAddresses.ToArray()), _default);
                IDnsQueryResponse txtResponse = recursiveClient.LookupClient.Query(cname.CanonicalName, QueryType.TXT);
                _log.Debug("Name server {NameServerIpAddress} selected", txtResponse.NameServer.Endpoint.Address.ToString());
                return recursiveClient.RecursivelyFollowCnames(txtResponse);
            }
            return result;
        }
    }
}