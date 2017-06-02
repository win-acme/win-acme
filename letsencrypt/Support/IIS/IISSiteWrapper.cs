using System;
using System.Collections.Generic;
using Microsoft.Web.Administration;

namespace letsencrypt.Support
{
    internal class IISSiteWrapper : IIISSite
    {
        private Site site;

        public IISSiteWrapper(Site site)
        {
            this.site = site;
        }

        public IEnumerable<IIISBinding> Bindings
        {
            get
            {
                foreach (var b in site.Bindings)
                {
                    yield return new IISBindingWrapper(b);
                }
            }
        }

        public long Id
        {
            get
            {
                return site.Id;
            }
        }

        public string Name
        {
            get
            {
                return site.Name;
            }
        }

        public IIISBinding AddBinding(string bindingInformation, byte[] certificateHash, string certificateStoreName)
        {
            return new IISBindingWrapper(site.Bindings.Add(bindingInformation, certificateHash, certificateStoreName));
        }

        public IIISBinding AddBinding(string bindingInformation, string bindingProtocol)
        {
            return new IISBindingWrapper(site.Bindings.Add(bindingInformation, bindingProtocol));
        }

        public string GetPhysicalPath()
        {
            return site.Applications["/"].VirtualDirectories["/"].PhysicalPath;
        }
    }
}