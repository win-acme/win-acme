using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;
using Microsoft.Win32;

namespace letsencrypt.Support
{
    internal class IISServerManagerWrapper : IIISServerManager
    {
        private ServerManager manager;

        public IEnumerable<IIISSite> Sites
        {
            get
            {
                return GetSites();
            }
        }

        private IEnumerable<IIISSite> GetSites()
        {
            var sites = GetManager().Sites;
            foreach(var site in sites)
            {
                yield return new IISSiteWrapper(site);
            }
        }

        private ServerManager GetManager()
        {
            if (manager == null)
            {
                manager = new ServerManager();
            }
            return manager;
        }

        public void CommitChanges()
        {
            GetManager().CommitChanges();
        }

        public void Dispose()
        {
            if (manager != null)
            {
                manager.Dispose();
                manager = null;
            }
        }

        public Version GetVersion()
        {
            var iisVersion = new Version(0, 0);
            using (RegistryKey inetStpKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
            {
                if (inetStpKey != null)
                {
                    int majorVersion = (int)inetStpKey.GetValue("MajorVersion", -1);
                    int minorVersion = (int)inetStpKey.GetValue("MinorVersion", -1);

                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        iisVersion = new Version(majorVersion, minorVersion);
                    }
                }
            }
            return iisVersion;
        }
    }
}
