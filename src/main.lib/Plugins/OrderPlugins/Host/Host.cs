using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    [IPlugin.Plugin<
        HostOptions, PluginOptionsFactory<HostOptions>, 
        HostCapability, WacsJsonPlugins>
        ("874a86e4-29c7-4294-9ab6-6908866847a0", 
        "Host", "Separate certificate for each host (e.g. sub.example.com)")]
    class Host : IOrderPlugin
    {
        public IEnumerable<Order> Split(Renewal renewal, Target target) 
        {
            var ret = new List<Order>();
            var seen = new List<Identifier>();
            foreach (var part in target.Parts)
            {
                foreach (var host in part.GetIdentifiers(true))
                {
                    if (!seen.Contains(host))
                    {
                        var parts = target.Parts.Where(p => p.GetIdentifiers(true).Contains(host));
                        var newTarget = new Target(
                            target.FriendlyName ?? "",
                            host,
                            parts.Select(p => 
                                new TargetPart(new List<Identifier> { host }) { 
                                    SiteId = p.SiteId,
                                    SiteType = p.SiteType 
                                }).ToList());
                        var newOrder = new Order(
                            renewal, 
                            newTarget, 
                            friendlyNamePart: host.Value,
                            cacheKeyPart: host.Value);
                        ret.Add(newOrder);
                        seen.Add(host);
                    }
                }
            }
            return ret;
        }
    }
}
