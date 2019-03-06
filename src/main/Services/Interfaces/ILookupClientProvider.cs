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
        /// Caches <see cref="LookupClient"/>s by <see cref="IPAddress"/>. Use <see cref="Default"/> instead if a specific <see cref="IPAddress"/> is not required.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        ILookupClient GetOrAdd(IPAddress ipAddress);
    }
}