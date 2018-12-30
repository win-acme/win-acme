using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.Administration;
using PKISharp.WACS.Extensions;

namespace PKISharp.WACS.Clients.IIS
{
    internal class IISSite : IIISSite<IISBinding>
    {
        internal Site Site { get; }

        public long Id => Site.Id;
        public string Name => Site.Name;
        public string Path => Site.WebRoot();

        IEnumerable<IIISBinding> IIISSite.Bindings => Bindings;
        public IEnumerable<IISBinding> Bindings { get; private set; }

     
        public IISSite(Site site)
        {
            Site = site;

            Bindings = site.Bindings.Select(x => new IISBinding(x));
        }
    }

    internal class IISBinding : IIISBinding
    {
        internal Binding Binding { get; }

        public string Host => Binding.Host;
        public string Protocol => Binding.Protocol;

        public IISBinding(Binding binding)
        {
            Binding = binding;
        }
    }
}
