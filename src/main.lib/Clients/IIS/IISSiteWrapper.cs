using Microsoft.Web.Administration;
using PKISharp.WACS.Extensions;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    /// <summary>
    /// Standard real implementation for IIS-site. Other 
    /// </summary>
    [DebuggerDisplay("Site {Id}")]
    internal class IISSiteWrapper : IIISSite<IISBindingWrapper>
    {
        internal Site Site { get; }

        public long Id => Site.Id;
        public string Name => Site.Name;
        public string Path => Site.WebRoot();

        IEnumerable<IIISBinding> IIISSite.Bindings => Bindings;
        public IEnumerable<IISBindingWrapper> Bindings { get; private set; }
        public IISSiteType Type { get; private set; } = IISSiteType.Unknown;

        public IISSiteWrapper(Site site)
        {
            var ftpSsl = !string.IsNullOrWhiteSpace(site.
                GetChildElement("ftpServer")?.
                GetChildElement("security")?.
                GetChildElement("ssl")?.
                GetAttributeValue("serverCertHash")?.
                ToString());

            Site = site;
            Bindings = site.Bindings.Select(x =>
            {
                var secure = x.Protocol == "https" || ((x.Protocol == "ftp") && ftpSsl);
                return new IISBindingWrapper(x, secure);
            }).ToList();

            if (Bindings.All(b => b.Protocol == "ftp" || b.Protocol == "ftps"))
            {
                Type = IISSiteType.Ftp;
            } 
            else if (Bindings.Any(b => b.Protocol == "http" || b.Protocol == "https"))
            {
                Type = IISSiteType.Web;
            }
        }
    }

    [DebuggerDisplay("{BindingInformation}")]
    internal class IISBindingWrapper : IIISBinding
    {
        internal Binding Binding { get; }

        public string Host 
        {
            get
            {
                if (Binding.Protocol == "ftp")
                {
                    var split = BindingInformation.Split(":");
                    if (split.Length == 3)
                    {
                        return split[2];
                    }
                }
                return Binding.Host;
            }
        }

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
        public bool Secure { get; private set; }

        public IISBindingWrapper(Binding binding, bool secure)
        {
            Binding = binding;
            Secure = secure;
        }
    }
}