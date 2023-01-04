using PKISharp.WACS.DomainObjects;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Interfaces
{
    public interface IOrderPlugin : IPlugin
    {
        IEnumerable<Order> Split(Renewal renewal, Target target);
    }
}
