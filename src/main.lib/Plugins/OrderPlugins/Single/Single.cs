using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Interfaces;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.OrderPlugins
{
    [IPlugin.Plugin<SingleOptions, SingleOptionsFactory>
        ("b705fa7c-1152-4436-8913-e433d7f84c82", "Single", "Single certificate")]
    class Single : IOrderPlugin
    {
        public IEnumerable<Order> Split(Renewal renewal, Target target) => new List<Order>() { new Order(renewal, target) };
    }
}
