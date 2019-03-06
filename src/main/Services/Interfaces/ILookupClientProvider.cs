using System.Net;
using DnsClient;

namespace PKISharp.WACS.Services.Interfaces
{
    public interface ILookupClientProvider
    {
        /// <summary>
        /// The default <see cref="LookupClient"/>. Internally uses your local network DNS.
        /// </summary>
        ILookupClient Default { get; }

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by <see cref="IPAddress"/>. Use <see cref="Default"/> instead if a specific name server is not required.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using the specified <see cref="IPAddress"/>.</returns>
        ILookupClient GetOrAdd(IPAddress ipAddress);

        /// <summary>
        /// Caches <see cref="LookupClient"/>s by domainName. Use <see cref="LookupClientProvider.Default"/> instead if a name server for a specific domain name is not required.
        /// </summary>
        /// <param name="domainName"></param>
        /// <returns>Returns an <see cref="ILookupClient"/> using a name server associated with the specified domain name.</returns>
        ILookupClient GetOrAdd(string domainName);
    }
}