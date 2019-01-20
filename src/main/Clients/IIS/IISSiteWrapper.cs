using System.Collections.Generic;
using System.Linq;
using Microsoft.Web.Administration;
using PKISharp.WACS.Extensions;

namespace PKISharp.WACS.Clients.IIS
{
    /// <summary>
    /// Standard real implementation for IIS-site. Other 
    /// </summary>
    internal class IISSiteWrapper : IIISSite<IISBindingWrapper>
    {
        internal Site Site { get; }

        public long Id => Site.Id;
        public string Name => Site.Name;
        public string Path => Site.WebRoot();

        IEnumerable<IIISBinding> IIISSite.Bindings => Bindings;
        public IEnumerable<IISBindingWrapper> Bindings { get; private set; }

     
        public IISSiteWrapper(Site site)
        {
            Site = site;

            Bindings = site.Bindings.Select(x => new IISBindingWrapper(x));
        }
    }

    internal class IISBindingWrapper : IIISBinding
    {
        internal Binding Binding { get; }

        public string Host => Binding.Host;
        public string Protocol => Binding.Protocol;

        public IISBindingWrapper(Binding binding)
        {
            Binding = binding;
        }
    }
}