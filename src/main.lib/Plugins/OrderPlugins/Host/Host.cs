using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
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
                            parts.Select(p => new TargetPart(new List<Identifier> { host }) { SiteId = p.SiteId }));
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
