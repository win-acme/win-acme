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

        public Task<Target> Generate()
        {
            return Task.FromResult(new Target()
            {
                FriendlyName = $"[{nameof(Manual)}] {_options.CommonName}",
                CommonName = _options.CommonName,
                Parts = new List<TargetPart> {
                    new TargetPart {
                        Identifiers = _options.AlternativeNames
                    }
                }
            });
        }

        bool ITargetPlugin.Disabled => false;
    }
}