using DnsClient;
using PKISharp.WACS.Extensions;
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
        private readonly Lazy<IEnumerable<LookupClientWrapper>> _defaultLookupClients;
        private readonly ConcurrentDictionary<string, LookupClientWrapper> _lookupClients = new ConcurrentDictionary<string, LookupClientWrapper>();

        private readonly ILogService _log;
        private readonly ISettingsService _settings;
        public DomainParseService DomainParser { get; private set; }

        public LookupClientProvider(
            DomainParseService domainParser,
            ILogService logService,
            ISettingsService settings)
        {
            DomainParser = domainParser;
            _defaultLookupClients = new Lazy<IEnumerable<LookupClientWrapper>>(() => ParseDefaultClients(domainParser, logService));
            _log = logService;
            _settings = settings;
        }

        private List<LookupClientWrapper> ParseDefaultClients(DomainParseService domainParser, ILogService logService)
        {
            var ret = new List<LookupClientWrapper>();
            var items = _settings.DnsServer.ParseCsv();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (IPAddress.TryParse(item, out var ip))
                    {
                        _log.Debug("Adding {ip} as DNS server", ip);
                        ret.Add(GetClient(ip));
                    }
                    else if (!string.IsNullOrEmpty(item))
                    {
                        if (item.Equals("[System]", StringComparison.OrdinalIgnoreCase))
                        {
                            ret.Add(new LookupClientWrapper(domainParser, logService, null, this));
                        }
                        else
                        {
                            var tempClient = new LookupClient();
                            var queryResult = tempClient.GetHostEntry(item);
                            var address = queryResult.AddressList.FirstOrDefault();
                            if (address != null)
                            {
                                _log.Debug("Adding {item} ({ip}) as DNS server", address);
                                ret.Add(GetClient(address));
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
                ret.Add(new LookupClientWrapper(domainParser, logService, null, this));
            }
            return ret;
        }

        /// <summary>
        /// The default <see cref="LookupClient"/>. Internally uses your local network DNS.
        /// </summary>
        public LookupClientWrapper GetDefaultClient(int round)
        {
            var index = round % _defaultLookupClients.Value.Count();
            var ret = _defaultLookupClients.Value.ElementAt(index);
            return ret;
        }

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by <see cref="IPAddress"/>. Use <see cref="DefaultClient"/> instead if a specific name server is not required.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using the specified <see cref="IPAddress"/>.</returns>
        public LookupClientWrapper GetClient(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }
            return _lookupClients.GetOrAdd(ipAddress.ToString(), new LookupClientWrapper(DomainParser, _log, ipAddress, this));
        }

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by domainName. Use <see cref="DefaultClient"/> instead if a name server for a specific domain name is not required.
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using a name server associated with the specified domain name.</returns>
        public async Task<LookupClientWrapper> GetClient(string domainName, int round = 0)
        {
            // _acme-challenge.sub.example.co.uk
            domainName = domainName.TrimEnd('.');

            // First domain we should try to ask 
            var rootDomain = DomainParser.GetRegisterableDomain(domainName);
            var testZone = rootDomain;
            var authoritativeZone = testZone;
            var client = GetDefaultClient(round);
            _log.Debug("Start looking for authoritative DNS server from {ip}", client.IpAddress?.ToString() ?? "[System]");

            // Other sub domains we should try asking:
            // 1. sub
            // 2. _acme-challenge
            IEnumerable<string> remainingParts = domainName.Substring(0, domainName.LastIndexOf(rootDomain)).Trim('.').Split('.');
            remainingParts = remainingParts.Reverse();

            var digDeeper = true;
            IEnumerable<IPAddress> ipSet = null;
            do
            {
                using (LogContext.PushProperty("Domain", testZone))
                {
                    _log.Verbose("Querying name servers for {part}", testZone);
                    var tempResult = await client.GetAuthoritativeNameServers(testZone, round);
                    if (tempResult != null)
                    {
                        ipSet = tempResult;
                        authoritativeZone = testZone;
                        client = GetClient(ipSet.First());
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
            } while (digDeeper);

            if (ipSet == null)
            {
                throw new Exception($"Unable to determine name servers for domain {domainName}");
            }

            return _lookupClients.GetOrAdd(authoritativeZone, new LookupClientWrapper(DomainParser, _log, ipSet.First(), this));
        }

    }
}