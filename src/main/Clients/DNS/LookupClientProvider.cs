using DnsClient;
using Nager.PublicSuffix;
using PKISharp.WACS.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.Clients.DNS
{
    public class LookupClientProvider
    {
        private readonly Lazy<LookupClientWrapper> _defaultLookupClient;
        private readonly ConcurrentDictionary<string, LookupClientWrapper> _lookupClients = new ConcurrentDictionary<string, LookupClientWrapper>();

        private readonly ILogService _log;
        public DomainParser DomainParser { get; private set; }

        public LookupClientProvider(
            DomainParser domainParser,
            ILogService logService)
        {
            DomainParser = domainParser;
            _defaultLookupClient = new Lazy<LookupClientWrapper>(() => new LookupClientWrapper(domainParser, logService, new LookupClient(), this));
            _log = logService;
        }

        /// <summary>
        /// The default <see cref="LookupClient"/>. Internally uses your local network DNS.
        /// </summary>
        public LookupClientWrapper DefaultClient => _defaultLookupClient.Value;

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
            return _lookupClients.GetOrAdd(ipAddress.ToString(), new LookupClientWrapper(DomainParser, _log, new LookupClient(ipAddress), this));
        }

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by domainName. Use <see cref="DefaultClient"/> instead if a name server for a specific domain name is not required.
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using a name server associated with the specified domain name.</returns>
        public LookupClientWrapper GetClient(string domainName)
        {
            IPAddress[] ipAddresses = DefaultClient.GetAuthoritativeNameServers(domainName, out string authoratitiveZone).ToArray();
            return _lookupClients.GetOrAdd(authoratitiveZone, new LookupClientWrapper(DomainParser, _log, new LookupClient(ipAddresses), this));
        }

    }
}