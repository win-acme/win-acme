using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    class Single : IOrderPlugin
    {
        public IEnumerable<Order> Split(Renewal renewal, Target target) => new List<Order>() { new Order(renewal, target) };
    }
}
