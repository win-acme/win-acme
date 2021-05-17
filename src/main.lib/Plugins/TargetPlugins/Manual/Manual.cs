using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Linq;
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
                    new TargetPart(_options.AlternativeNames.Select(x => new DnsIdentifier(x)))
                });
        }

        (bool, string?) IPlugin.Disabled => (false, null);
    }
}