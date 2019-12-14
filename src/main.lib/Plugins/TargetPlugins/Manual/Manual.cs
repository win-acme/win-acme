using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class Manual : ITargetPlugin
    {
        private readonly ManualOptions _options;

        public Manual(ManualOptions options) => _options = options;

        public async Task<Target?> Generate()
        {
            return new Target()
            {
                FriendlyName = $"[{nameof(Manual)}] {_options.CommonName}",
                CommonName = _options.CommonName,
                Parts = new List<TargetPart> {
                    new TargetPart(_options.AlternativeNames)
                }
            };
        }

        bool IPlugin.Disabled => false;
    }
}