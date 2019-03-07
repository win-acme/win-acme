using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using DnsClient;
using Nager.PublicSuffix;
using PKISharp.WACS.Services.Interfaces;
using Serilog.Context;

namespace PKISharp.WACS.Services
{
    public class LookupClientProvider : ILookupClientProvider
    {
        private static readonly Lazy<ILookupClient> _defaultLookupClient = new Lazy<ILookupClient>(() => new LookupClient());
        private readonly ConcurrentDictionary<string, ILookupClient> _lookupClients = new ConcurrentDictionary<string, ILookupClient>();
        private readonly DomainParser _domainParser;
        private readonly ILogService _log;

        public LookupClientProvider(DomainParser domainParser, ILogService logService)
        {
            _domainParser = domainParser;
            _log = logService;
        }

        /// <summary>
        /// The default <see cref="LookupClient"/>. Internally uses your local network DNS.
        /// </summary>
        public ILookupClient Default => _defaultLookupClient.Value;

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by <see cref="IPAddress"/>. Use <see cref="Default"/> instead if a specific name server is not required.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using the specified <see cref="IPAddress"/>.</returns>
        public ILookupClient GetOrAdd(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            return _lookupClients.GetOrAdd(ipAddress.ToString(), new LookupClient(ipAddress));
        }

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by domainName. Use <see cref="Default"/> instead if a name server for a specific domain name is not required.
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using a name server associated with the specified domain name.</returns>
        public ILookupClient GetOrAdd(string domainName)
        {
            var rootDomain = _domainParser.Get(domainName).RegistrableDomain;
            return _lookupClients.GetOrAdd(rootDomain, LookupClientFactory(rootDomain));
        }

        private ILookupClient LookupClientFactory(string rootDomain)
        {
            var nameServers = GetIpAddresses(rootDomain);
            return new LookupClient(nameServers.ToArray());
        }

        private IEnumerable<IPAddress> GetIpAddresses(string rootDomain)
        {
            using (LogContext.PushProperty("Domain", rootDomain))
            {
                _log.Debug("Querying name servers");
                var nsResponse = Default.Query(rootDomain, QueryType.NS);

                foreach (var nsRecord in nsResponse.Answers.NsRecords())
                {
                    using (LogContext.PushProperty("NameServer", nsRecord.NSDName))
                    {
                        _log.Debug("Querying IP for name server");
                        var aResponse = Default.Query(nsRecord.NSDName, QueryType.A);

                        var nameServerIp = aResponse.Answers.ARecords().FirstOrDefault()?.Address;
                        _log.Debug("Name server IP {NameServerIpAddress} identified", nameServerIp);

                        yield return nameServerIp;
                    }
                }
            }
        }
    }
}