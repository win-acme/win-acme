using DnsClient;
using PKISharp.WACS.Services;
using Serilog.Context;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Clients.DNS
{
    public class LookupClientProvider
    {
        private readonly List<IPAddress> _defaultNs;
        private readonly ConcurrentDictionary<string, IEnumerable<IPAddress>> _authoritativeNs;
        private readonly ConcurrentDictionary<string, string?> _cnames;
        private readonly ConcurrentDictionary<IPAddress, LookupClientWrapper> _lookupClients;

        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        private readonly DomainParseService _domainParser;

        public LookupClientProvider(
            DomainParseService domainParser,
            ILogService logService,
            ISettingsService settings)
        {
            _log = logService;
            _settings = settings;
            _domainParser = domainParser;
            _authoritativeNs = new ConcurrentDictionary<string, IEnumerable<IPAddress>>();
            _cnames = new ConcurrentDictionary<string, string?>();
            _lookupClients = new ConcurrentDictionary<IPAddress, LookupClientWrapper>();
            _defaultNs = ParseDefaultClients();

        }

        /// <summary>
        /// Convert configured servers to a list of IP addresses
        /// </summary>
        /// <returns></returns>
        private List<IPAddress> ParseDefaultClients()
        {
            var ret = new List<IPAddress>();
            var items = _settings.Validation.DnsServers;
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (IPAddress.TryParse(item, out var ip))
                    {
                        _log.Verbose("Adding {ip} as DNS server", ip);
                        ret.Add(ip);
                    }
                    else if (!string.IsNullOrEmpty(item))
                    {
                        if (item.Equals("[System]", StringComparison.OrdinalIgnoreCase))
                        {
                            ret.Add(new IPAddress(0));
                        }
                        else
                        {
                            var tempClient = new LookupClient();
                            var queryResult = tempClient.GetHostEntry(item);
                            var address = queryResult.AddressList.FirstOrDefault();
                            if (address != null)
                            {
                                _log.Verbose("Adding {item} ({ip}) as DNS server", address);
                                ret.Add(address);
                            }
                            else
                            {
                                _log.Warning("IP for DNS server {item} could not be resolved", address);
                            }
                        }
                    }
                }
            }
            if (ret.Count == 0)
            {
                _log.Debug("Adding local system default as DNS server");
                ret.Add(new IPAddress(0));
            }
            return ret;
        }

        /// <summary>
        /// Produce a new LookupClientWrapper or take a previously 
        /// cached one from the dictionary
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private LookupClientWrapper Produce(IPAddress ip) => _lookupClients.GetOrAdd(ip, (ip) => new LookupClientWrapper(_log, ip.Equals(new IPAddress(0)) ? null : ip, this));

        /// <summary>
        /// Get clients for all default DNS servers
        /// </summary>
        /// <returns></returns>
        public List<LookupClientWrapper> GetDefaultClients() => _defaultNs.Select(x => Produce(x)).ToList();

        /// <summary>
        /// The default <see cref="LookupClient"/>. Internally uses your local network DNS.
        /// </summary>
        public LookupClientWrapper GetDefaultClient(int round)
        {
            var index = round % GetDefaultClients().Count();
            var ret = GetDefaultClients().ElementAt(index);
            return ret;
        }

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

        /// <summary>
        /// Get cached list of authoritative name server ip addresses
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public async Task<DnsLookupResult> GetAuthority(string domainName, int round = 0, bool followCnames = true, DnsLookupResult? from = null)
        {
            var key = domainName.ToLower().TrimEnd('.');
            try
            {
                // Example: _acme-challenge.sub.example.co.uk
                domainName = domainName.TrimEnd('.');

                // First domain we should try to ask is the tld (e.g. co.uk)
                var rootDomain = _domainParser.GetTLD(domainName);
                var testZone = rootDomain;
                var client = GetDefaultClient(round);

                // Other sub domains we should ask:
                // 1. example
                // 1. sub
                // 2. _acme-challenge
                var remainingParts = domainName.Substring(0, domainName.LastIndexOf(rootDomain))
                    .Trim('.').Split('.')
                    .Where(x => !string.IsNullOrEmpty(x));
                remainingParts = remainingParts.Reverse();

                var digDeeper = true;
                IEnumerable<IPAddress>? ipSet = null;
                do
                {
                    // Partial result cachign
                    if (!_authoritativeNs.ContainsKey(testZone))
                    {
                        _log.Verbose("Querying server {server} about {part}", client.IpAddress, testZone);
                        using (LogContext.PushProperty("Domain", testZone))
                        {
                            var tempResult = await client.GetNameServers(testZone, round);
                            _authoritativeNs.TryAdd(testZone, tempResult?.ToList() ?? ipSet ?? _defaultNs);
                        }
                    }
                    ipSet = _authoritativeNs[testZone];
                    client = Produce(ipSet.OrderBy(x => Guid.NewGuid()).First());  
                        
                    // CNAME only valid for full domain. Subdomains may be 
                    // regular records again
                    if (followCnames && testZone == key)
                    {
                        var cname = default(string?);
                        if (!_cnames.ContainsKey(key))
                        {
                            _cnames.TryAdd(key, await client.GetCname(testZone));
                        } 
                        cname = _cnames[key];
                        if (cname != null)
                        {                          
                            return await GetAuthority(cname, round, true, Produce(key, from));
                        }
                    }
       
                    if (remainingParts.Any())
                    {
                        testZone = $"{remainingParts.First()}.{testZone}";
                        remainingParts = remainingParts.Skip(1).ToArray();
                    }
                    else
                    {
                        digDeeper = false;
                    }
                }
                while (digDeeper);

                if (ipSet == null)
                {
                    throw new Exception("No results");
                }
            }
            catch (Exception ex)
            {
                _log.Warning("Unable to find or contact authoritative name servers for {domainName}: {message}", domainName, ex.Message);
                _authoritativeNs.TryAdd(key, _defaultNs);
            }
            return Produce(key, from);
        }

        private DnsLookupResult Produce(string key, DnsLookupResult? parent = null) => new DnsLookupResult(key, _authoritativeNs[key].Select(ip => Produce(ip)), parent);
    }
}