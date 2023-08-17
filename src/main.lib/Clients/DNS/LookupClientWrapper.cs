using DnsClient;
using DnsClient.Protocol;
using PKISharp.WACS.Services;
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
        private readonly LookupClientWrapper _system;
        private readonly IPAddress? _ipAddress;
        private readonly ILookupClient _lookupClient;

        private bool? _connected = null;
        public string IpAddress => _ipAddress?.ToString() ?? "[System]";

        /// <summary>
        /// Construct system DNS
        /// </summary>
        /// <param name="logService"></param>
        /// <param name="ipAddress"></param>
        /// <param name="system"></param>
        public LookupClientWrapper(ILogService logService, IEnumerable<IPAddress>? ipAddress)
        {
            _ipAddress = ipAddress?.FirstOrDefault();
            var clientOptions = ipAddress != null && ipAddress.Any() ?
                new LookupClientOptions(ipAddress.ToArray()) :
                new LookupClientOptions();
            clientOptions.UseCache = false;
            _lookupClient = new LookupClient(clientOptions);
            _log = logService;
            _system = this;
        }

        /// <summary>
        /// Regular wrapper for specific name server
        /// </summary>
        /// <param name="logService"></param>
        /// <param name="ipAddress"></param>
        /// <param name="system"></param>
        public LookupClientWrapper(ILogService logService, IPAddress ipAddress, LookupClientWrapper system)
        {
            _ipAddress = ipAddress;
            var clientOptions = new LookupClientOptions(new[] { _ipAddress })
            {
                UseCache = false
            };
            _lookupClient = new LookupClient(clientOptions);
            _log = logService;
            _system = system;
        }

        public async Task<IEnumerable<IPAddress>> GetNameServers(string host)
        {
            host = host.TrimEnd('.');
            _log.Debug("Querying name servers for {part}", host);
            var nsResponse = await _lookupClient.QueryAsync(host, QueryType.NS);
            if (nsResponse.HasError)
            {
                _log.Verbose("Error from {server}: {message}", IpAddress, nsResponse.ErrorMessage);
            }
            var nsRecords = nsResponse.Answers.NsRecords();
            var cnameRecords = nsResponse.Answers.CnameRecords();
            if (!nsRecords.Any() && !cnameRecords.Any())
            {
                nsRecords = nsResponse.Authorities.OfType<NsRecord>();
            }
            if (nsRecords.Any())
            {
                var nsHosts = nsRecords.Select(n => n.NSDName.Value);
                _log.Verbose("Found nsRecords: {nsRecord}", nsHosts);
                return GetIpAddresses(nsHosts);
            }
            return new List<IPAddress>();
        }

        private IEnumerable<IPAddress> GetIpAddresses(IEnumerable<string> hosts)
        {
            var ret = new List<IPAddress>();    
            foreach (var nsRecord in hosts)
            {
                _log.Verbose("Querying IP for name server {nsRecord}", nsRecord);
                var aResponse = _system._lookupClient.Query(nsRecord, QueryType.A);
                var nameServerIp = aResponse.Answers.ARecords().FirstOrDefault()?.Address;
                if (nameServerIp != null)
                {
                    ret.Add(nameServerIp);
                    _log.Verbose("Name server IP {NameServerIpAddress} identified", nameServerIp);
                }
            }
            return ret;
        }

        /// <summary>
        /// Verify that we can connect to the main server
        /// </summary>
        /// <returns></returns>
        public async Task<bool> Connect()
        {
            if (_connected == null) 
            {
                try
                {
                    await GetTxtRecords("www.example.com");
                    _connected = true;
                }
                catch
                {
                    _log.Warning("Error connection to {ip}", IpAddress);
                    _connected = false;
                }
            }
            return _connected == true;
        }

        public async Task<IEnumerable<string>> GetTxtRecords(string host)
        {
            var txtResult = await _lookupClient.QueryAsync(host, QueryType.TXT);
            if (txtResult.HasError)
            {
                _log.Verbose("Error from {server}: {message}", IpAddress, txtResult.ErrorMessage);
            }
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
            if (ipv4.HasError)
            {
                _log.Verbose("Error from {server}: {message}", IpAddress, ipv4.ErrorMessage);
            }
            ret.AddRange(ipv4.Answers.ARecords().
                Where(aRecord => aRecord != null).
                Select(aRecord => aRecord.Address).
                Where(ip => ip != null).
                OfType<IPAddress>());
            var ipv6 = await _lookupClient.QueryAsync(host, QueryType.AAAA);
            if (ipv6.HasError)
            {
                _log.Verbose("Error from {server}: {message}", IpAddress, ipv6.ErrorMessage);
            }
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
            if (cnames.HasError)
            {
                _log.Verbose("Error from {server}: {message}", IpAddress, cnames.ErrorMessage);
            }
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