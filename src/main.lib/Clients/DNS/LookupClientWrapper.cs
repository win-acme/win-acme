using DnsClient;
using DnsClient.Protocol;
using PKISharp.WACS.Services;
using Serilog.Context;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients.DNS
{
    public class LookupClientWrapper
    {
        private readonly ILogService _log;
        private readonly LookupClientProvider _provider;
        private readonly IPAddress? _ipAddress;

        private ILookupClient _lookupClient { get; set; }
        public string IpAddress => _ipAddress?.ToString() ?? "[System]";

        public LookupClientWrapper(ILogService logService, IPAddress? ipAddress, LookupClientProvider provider)
        {
            _ipAddress = ipAddress;
            var clientOptions = _ipAddress != null ?
                new LookupClientOptions(new[] { _ipAddress }) : 
                new LookupClientOptions();
            clientOptions.UseCache = false;
            _lookupClient = new LookupClient(clientOptions);
            _log = logService;
            _provider = provider;
        }

        public async Task<IEnumerable<IPAddress>?> GetNameServers(string host, int round)
        {
            host = host.TrimEnd('.');
            _log.Debug("Querying name servers for {part}", host);
            var nsResponse = await _lookupClient.QueryAsync(host, QueryType.NS);
            var nsRecords = nsResponse.Answers.NsRecords();
            var cnameRecords = nsResponse.Answers.CnameRecords();
            if (!nsRecords.Any() && !cnameRecords.Any())
            {
                nsRecords = nsResponse.Authorities.OfType<NsRecord>();
            }
            if (nsRecords.Any())
            {
                return GetIpAddresses(nsRecords.Select(n => n.NSDName.Value), round);
            }
            //if (cnameRecords.Any())
            //{
            //    var client = await _provider.GetClients(cnameRecords.First().CanonicalName.Value);
            //    return await _provider.GetAuthoritativeNameServersForDomain(cnameRecords.First().CanonicalName.Value, round);
            //}
            return null;
        }

        private IEnumerable<IPAddress> GetIpAddresses(IEnumerable<string> hosts, int round)
        {
            foreach (var nsRecord in hosts)
            {
                using (LogContext.PushProperty("NameServer", nsRecord))
                {
                    _log.Verbose("Querying IP for name server");
                    var aResponse = _provider.GetDefaultClient(round)._lookupClient.Query(nsRecord, QueryType.A);
                    var nameServerIp = aResponse.Answers.ARecords().FirstOrDefault()?.Address;
                    if (nameServerIp != null)
                    {
                        _log.Verbose("Name server IP {NameServerIpAddress} identified", nameServerIp);
                        yield return nameServerIp;
                    }
                }
            }
        }

        public async Task<IEnumerable<string>> GetTxtRecords(string host)
        {
            var txtResult = await _lookupClient.QueryAsync(host, QueryType.TXT);
            return txtResult.Answers.TxtRecords().
                Where(txtRecord => txtRecord != null).
                SelectMany(txtRecord => txtRecord.EscapedText).
                Where(txtRecord => txtRecord != null).
                OfType<string>().
                ToList();
        }

        public async Task<IEnumerable<IPAddress>> GetIps(string host)
        {
            var ret = new List<IPAddress>();
            var ipv4 = await _lookupClient.QueryAsync(host, QueryType.A);
            ret.AddRange(ipv4.Answers.ARecords().
                Where(aRecord => aRecord != null).
                Select(aRecord => aRecord.Address).
                Where(ip => ip != null).
                OfType<IPAddress>());
            var ipv6 = await _lookupClient.QueryAsync(host, QueryType.AAAA);
            ret.AddRange(ipv6.Answers.AaaaRecords().
                Where(aRecord => aRecord != null).
                Select(aRecord => aRecord.Address).
                Where(ip => ip != null).
                OfType<IPAddress>());
            return ret;
        }

        public async Task<string?> GetCname(string host)
        {
            var cnames = await _lookupClient.QueryAsync(host, QueryType.CNAME);
            var cnameRecords = cnames.Answers.CnameRecords();
            if (cnameRecords.Any())
            {
                var canonicalName = cnameRecords.First().CanonicalName.Value;
                var idn = new IdnMapping();
                canonicalName = canonicalName.ToLower().Trim().TrimEnd('.');
                canonicalName = idn.GetAscii(canonicalName);
                return canonicalName;
            }
            return null;
        }
    }
}