using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    [IPlugin.Plugin<
        SiteOptions, PluginOptionsFactory<SiteOptions>, 
        SiteCapability, WacsJsonPlugins>
        ("74a42b2d-8eaa-4f40-ab6a-f55304254143", 
        "Site", "Separate certificate for each IIS site")]
    class Site : IOrderPlugin
    {
        public IEnumerable<Order> Split(Renewal renewal, Target target) 
        {
            var ret = new List<Order>();
            foreach (var part in target.Parts)
            {
                var commonName = target.CommonName;
                if (commonName != null && !part.Identifiers.Contains(commonName))
                {
                    commonName = part.Identifiers.Where(x => x.Value.Length <= Constants.MaxCommonName).FirstOrDefault();
                }
                var newTarget = new Target(
                    target.FriendlyName,
                    commonName,
                    new List<TargetPart> { part });
                var newOrder = new Order(
                    renewal, 
                    newTarget, 
                    friendlyNamePart: $"site {part.SiteId ?? -1}",
                    cacheKeyPart: $"{part.SiteId ?? -1}");
                ret.Add(newOrder);
            }
            return ret;
        }
    }
}
