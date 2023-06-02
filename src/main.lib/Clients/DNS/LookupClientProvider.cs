using DnsClient;
using PKISharp.WACS.Services;
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
        private readonly ConcurrentDictionary<string, IEnumerable<IPAddress>> _authoritativeNs;
        private readonly ConcurrentDictionary<string, string?> _cnames;
        private readonly ConcurrentDictionary<IPAddress, LookupClientWrapper> _lookupClients;
        private readonly LookupClientWrapper _systemClient;

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
            _systemClient = new LookupClientWrapper(_log, ParseDefaultClients());
        }

        /// <summary>
        /// Convert configured servers to a list of IP addresses
        /// </summary>
        /// <returns></returns>
        private List<IPAddress>? ParseDefaultClients()
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
                            _log.Debug("Adding local system default as DNS server");
                            return null;
                        }
                        else
                        {
                            var tempClient = new LookupClient();
                            var queryResult = tempClient.GetHostEntry(item);
                            var address = queryResult.AddressList.FirstOrDefault();
                            if (address != null)
                            {
                                _log.Verbose("Adding {item} ({ip}) as DNS server", item, address);
                                ret.Add(address);
                            }
                            else
                            {
                                _log.Warning("IP for DNS server {item} could not be resolved", item);
                            }
                        }
                    }
                }
            }
            if (ret.Count == 0)
            {
                _log.Debug("Adding local system default as DNS server");
                return null;
            }
            return ret;
        }

        /// <summary>
        /// Get client for the default DNS servers
        /// </summary>
        /// <returns></returns>
        public LookupClientWrapper GetSystemClient() => _systemClient;

        /// <summary>
        /// Get cached list of authoritative name server ip addresses
        /// </summary>
        /// <param name="domainName"></param>
        /// <param name="round"></param>
        /// <returns></returns>
        public async Task<DnsLookupResult> GetAuthority(string domainName, bool followCnames = true, DnsLookupResult? from = null)
        {
            var result = await Produce(domainName, from);
            if (followCnames)
            {
                string? cname;
                if (!_cnames.ContainsKey(domainName))
                {
                    var pickNs = result.Nameservers.First();
                    _log.Verbose("Query CNAME for {domainName} at {pickNs}", domainName, pickNs.IpAddress);
                    cname = await pickNs.GetCname(domainName);
                    _cnames.TryAdd(domainName, cname);
                }
                else
                {
                    cname = _cnames[domainName];
                }
                if (cname != null)
                {
                    _log.Verbose("Following CNAME from {domainName} to {cname}", domainName, cname);
                    return await GetAuthority(cname, true, result);
                }
            }
            return result;
        }

        private async Task<IEnumerable<IPAddress>> GetNameServers(string domainName)
        {
            IEnumerable<IPAddress>? backup = null;
            try
            {
                // Example: _acme-challenge.sub.example.co.uk
                domainName = domainName.TrimEnd('.');

                // Subresult cache
                if (_authoritativeNs.TryGetValue(domainName, out var cached))
                {
                    return cached;
                }

                // Root domain is queried from the system client
                var rootDomain = _domainParser.GetRegisterableDomain(domainName);

                IEnumerable<IPAddress>? unverified;
                if (domainName == rootDomain)
                {
                    unverified = await _systemClient.GetNameServers(domainName);
                    if (!unverified.Any())
                    {
                        _log.Warning("Unable to find any name servers for {domainName}", domainName);
                        return unverified;
                    }
                }
                else
                {
                    // Get name servers from one level up, e.g. 
                    // if we are asked about a.b.c.d, we should
                    // get the name servers for b.c.d
                    var levelUp = string.Join('.', domainName.Split('.').Skip(1));
                    backup = await GetNameServers(levelUp);
                    LookupClientWrapper specifiedClient;
                    if (!backup.Any())
                    {
                        specifiedClient = _systemClient;
                    }
                    else
                    {
                        specifiedClient = Produce(backup.First());
                    }
                    unverified = await specifiedClient.GetNameServers(domainName);
                }

                // Test if the name servers are usable
                var verified = new List<IPAddress>();
                foreach (var candidate in unverified)
                {
                    var testClient = Produce(candidate);
                    if (await testClient.Connect())
                    {
                        verified.Add(candidate);
                    }
                }     
                if (!verified.Any())
                {
                    if (unverified.Any())
                    {
                        _log.Warning("Unable to contact name servers for {domainName}", domainName);
                    }
                    else
                    {
                        _log.Verbose("No specific name servers identified for {domainName}", domainName);
                    }
                    verified = backup?.ToList() ?? new List<IPAddress>();
                }
                _authoritativeNs.TryAdd(domainName, verified);
            }
            catch (Exception ex)
            {
                _log.Warning("Unexpected DNS error while checking {domainName}: {message}", domainName, ex.Message);
                _log.Verbose(ex.StackTrace ?? "No stacktrace");
                _authoritativeNs.TryAdd(domainName, new List<IPAddress>());
            }
            return _authoritativeNs[domainName];
        }

        /// <summary>
        /// Produce a new LookupClientWrapper or take a previously 
        /// cached one from the dictionary
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        private LookupClientWrapper Produce(IPAddress ip) => _lookupClients.GetOrAdd(ip, (ip) => new LookupClientWrapper(_log, ip, _systemClient));

        /// <summary>
        /// Produce a DnsLookupResult
        /// </summary>
        /// <param name="key"></param>
        /// <param name="parent"></param>
        /// <returns></returns>
        private async Task<DnsLookupResult> Produce(string key, DnsLookupResult? parent = null)
        {
            var nameServers = await GetNameServers(key);
            var clients = nameServers.Any() ?
                nameServers.Select(Produce) :
                new[] { _systemClient };
            return new(key, clients, parent);
        }
    }
}
