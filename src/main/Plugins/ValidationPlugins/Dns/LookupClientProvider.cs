using System;
using System.Collections.Concurrent;
using System.Net;
using DnsClient;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class LookupClientProvider : ILookupClientProvider
    {
        private static readonly Lazy<ILookupClient> _defaultLookupClient = new Lazy<ILookupClient>(() => new LookupClient());
        private readonly ConcurrentDictionary<IPAddress, ILookupClient> _lookupClients = new ConcurrentDictionary<IPAddress, ILookupClient>();

        /// <summary>
        /// The default <see cref="LookupClient"/>. Internally uses your local network DNS.
        /// </summary>
        public ILookupClient Default => _defaultLookupClient.Value;

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by <see cref="IPAddress"/>. Use <see cref="Default"/> instead if a specific <see cref="IPAddress"/> is not required.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public ILookupClient GetOrAdd(IPAddress ipAddress)
        {
            if (ipAddress == null)
            {
                throw new ArgumentNullException(nameof(ipAddress));
            }

            return _lookupClients.GetOrAdd(ipAddress, new LookupClient(ipAddress));
        }
    }
}