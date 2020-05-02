using Microsoft.Web.Administration;
using PKISharp.WACS.Extensions;
using System.Collections.Generic;
using System.Linq;

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

            Bindings = site.Bindings.Select(x => new IISBindingWrapper(x)).ToList();
        }
    }

    internal class IISBindingWrapper : IIISBinding
    {
        internal Binding Binding { get; }

        public string Host => Binding.Host;
        public string Protocol => Binding.Protocol;
        public int Port => Binding.EndPoint?.Port ?? -1;
        public string? IP 
        {
            get
            {
                var address = Binding.EndPoint?.Address;
                if (address == null || address.GetAddressBytes().All(b => b == 0))
                {
                    return null;
                } 
                else
                {
                    return address.ToString();
                }
            }
        }

        public byte[]? CertificateHash
        {
            get
            {
                try
                {
                    return Binding.CertificateHash;
                }
                catch
                {
                    return null;
                }
            }
        }

        public string CertificateStoreName => Binding.CertificateStoreName;
        public string BindingInformation => Binding.NormalizedBindingInformation();
        public SSLFlags SSLFlags => Binding.SSLFlags();

        public IISBindingWrapper(Binding binding) => Binding = binding;
    }
}