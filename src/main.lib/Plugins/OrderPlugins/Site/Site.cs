using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    class Site : IOrderPlugin
    {
        public IEnumerable<Order> Split(Renewal renewal, Target target) 
        {
            var ret = new List<Order>();
            foreach (var part in target.Parts)
            {
               var newTarget = new Target(
                    target.FriendlyName ?? "",
                    target.CommonName,
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
