using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class Manual : ITargetPlugin
    {
        private readonly ManualOptions _options;

        public Manual(ManualOptions options) => _options = options;

        public async Task<Target> Generate()
        {
            return new Target(
                $"[{nameof(Manual)}] {_options.CommonName}",
                _options.CommonName ?? "",
                new List<TargetPart> {
                    new TargetPart(_options.AlternativeNames.Select(x => ParseIdentifier(x)))
                });
        }

        public static Identifier ParseIdentifier(string identifier)
        {
            if (IPAddress.TryParse(identifier, out var address))
            {
                return new IpIdentifier(address);
            }
            return new DnsIdentifier(identifier);
        }

        (bool, string?) IPlugin.Disabled => (false, null);
    }
}