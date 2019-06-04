using Autofac;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    internal class IISClient : IIISClient<IISSiteWrapper, IISBindingWrapper>, IDisposable
    {
        public const int DefaultBindingPort = 443;
        public const string DefaultBindingIp = "*";

        public Version Version { get; set; }
        private ServerManager _ServerManager;
        private ILogService _log;

        public IISClient(ILogService log)
        {
            _log = log;
            Version = GetIISVersion();
        }

        /// <summary>
        /// Single reference to the ServerManager
        /// </summary>
        private ServerManager ServerManager
        {
            get
            {
                if (_ServerManager == null)
                {
                    if (Version.Major > 0)
                    {
                        _ServerManager = new ServerManager();
                    }
                }
                return _ServerManager;
            }
        }

        /// <summary>
        /// Commit changes to server manager and remove the 
        /// reference to the cached version because it might
        /// be the cause of some bug to keep using the same
        /// ServerManager to commit multiple changes
        /// </summary>
        void Commit()
        {
            if (_ServerManager != null)
            {
                _ServerManager.CommitChanges();
                _ServerManager = null;
            }
        }

        #region _ Basic retrieval _

        public bool HasWebSites
        {
            get
            {
                return Version.Major > 0 && WebSites.Count() > 0;
            }
        }

        IEnumerable<IIISSite> IIISClient.WebSites => WebSites;
        public IEnumerable<IISSiteWrapper> WebSites
        {
            get
            {
                return ServerManager.Sites.AsEnumerable().
                    Where(s => s.Bindings.Any(sb => sb.Protocol == "http" || sb.Protocol == "https")).
                    Where(s =>
                    {
                        try
                        {
                            return s.State == ObjectState.Started;
                        }
                        catch
                        {
                            // Prevent COMExceptions such as misconfigured
                            // application pools from crashing the whole 
                            _log.Warning("Unable to determine state for Site {id}", s.Id);
                            return false;
                        }
                    }).
                    OrderBy(s => s.Name).
                    Select(x => new IISSiteWrapper(x));
            }
        }

        IIISSite IIISClient.GetWebSite(long id) => GetWebSite(id);
        public IISSiteWrapper GetWebSite(long id)
        {
            foreach (var site in WebSites)
            {
                if (site.Site.Id == id) return site;
            }
            throw new Exception($"Unable to find IIS SiteId #{id}");
        }

        public bool HasFtpSites
        {
            get
            {
                return Version >= new Version(7, 5) && FtpSites.Count() > 0;
            }
        }

        IEnumerable<IIISSite> IIISClient.FtpSites => FtpSites;
        public IEnumerable<IISSiteWrapper> FtpSites
        {
            get
            {
                return ServerManager.Sites.AsEnumerable().
                    Where(s => s.Bindings.Any(sb => sb.Protocol == "ftp")).
                    OrderBy(s => s.Name).
                    Select(x => new IISSiteWrapper(x));
            }
        }

        IIISSite IIISClient.GetFtpSite(long id) => GetFtpSite(id);
        public IISSiteWrapper GetFtpSite(long id)
        {
            foreach (var site in FtpSites)
            {
                if (site.Site.Id == id) return site;
            }
            throw new Exception($"Unable to find IIS SiteId #{id}");
        }

        #endregion

        #region _ Https Install _

        public void AddOrUpdateBindings(IEnumerable<string> identifiers, BindingOptions bindingOptions, byte[] oldThumbprint)
        {
            var updater = new IISHttpBindingUpdater<IISSiteWrapper, IISBindingWrapper>(this, _log);
            var updated = updater.AddOrUpdateBindings(identifiers, bindingOptions, oldThumbprint);
            if (updated > 0)
            {
                _log.Information("Committing {count} {type} binding changes to IIS", updated, "https");
                Commit();
            }
            else
            {
                _log.Warning("No bindings have been changed");
            }
        }

        public void AddBinding(IISSiteWrapper site, BindingOptions options)
        {
            var newBinding = site.Site.Bindings.CreateElement("binding");
            newBinding.Protocol = "https";
            newBinding.BindingInformation = options.Binding;
            newBinding.CertificateStoreName = options.Store;
            newBinding.CertificateHash = options.Thumbprint;
            if (options.Flags > 0)
            {
                newBinding.SetAttributeValue("sslFlags", options.Flags);
            }
            site.Site.Bindings.Add(newBinding);
        }

        public void UpdateBinding(IISSiteWrapper site, IISBindingWrapper existingBinding, BindingOptions options)
        {
            // Replace instead of change binding because of #371
            var handled = new[] {
                "protocol",
                "bindingInformation",
                "sslFlags",
                "certificateStoreName",
                "certificateHash"
            };
            var replacement = site.Site.Bindings.CreateElement("binding");
            replacement.Protocol = existingBinding.Protocol;
            replacement.BindingInformation = existingBinding.BindingInformation;
            replacement.CertificateStoreName = options.Store;
            replacement.CertificateHash = options.Thumbprint;
            foreach (var attr in existingBinding.Binding.Attributes)
            {
                try
                {
                    if (!handled.Contains(attr.Name) && attr.Value != null)
                    {
                        replacement.SetAttributeValue(attr.Name, attr.Value);
                    }
                }
                catch (Exception ex)
                {
                    _log.Warning("Unable to set attribute {name} on new binding: {ex}", attr.Name, ex.Message);
                }
            }

            if (options.Flags > 0)
            {
                replacement.SetAttributeValue("sslFlags", options.Flags);
            }
            site.Site.Bindings.Remove(existingBinding.Binding);
            site.Site.Bindings.Add(replacement);
        }

        #endregion

        #region _ Ftps Install _

        /// <summary>
        /// Update binding for FTPS site
        /// </summary>
        /// <param name="FtpSiteId"></param>
        /// <param name="newCertificate"></param>
        /// <param name="oldCertificate"></param>
        public void UpdateFtpSite(long FtpSiteId, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            var ftpSites = FtpSites.ToList();
            var oldThumbprint = oldCertificate?.Certificate?.Thumbprint;
            var newThumbprint = newCertificate?.Certificate?.Thumbprint;
            var updated = 0;
            foreach (var ftpSite in ftpSites)
            {
                var sslElement = ftpSite.Site.GetChildElement("ftpServer").
                    GetChildElement("security").
                    GetChildElement("ssl");

                var currentThumbprint = sslElement.GetAttributeValue("serverCertHash").ToString();
                var update = false;
                if (ftpSite.Site.Id == FtpSiteId)
                {
                    if (string.Equals(currentThumbprint, newThumbprint, StringComparison.CurrentCultureIgnoreCase))
                    {
                        _log.Information(true, "No updated need for ftp site {name}", ftpSite.Site.Name);
                    }
                    else
                    {
                        update = true;
                    }
                }
                else if (string.Equals(currentThumbprint, oldThumbprint, StringComparison.CurrentCultureIgnoreCase))
                {
                    update = true;
                }
                if (update)
                {
                    sslElement.SetAttributeValue("serverCertHash", newThumbprint);
                    _log.Information(true, "Updating existing ftp site {name}", ftpSite.Site.Name);
                    updated += 1;
                }
            }
            if (updated > 0)
            {
                _log.Information("Committing {count} {type} site changes to IIS", updated, "ftp");
                Commit();
            }
        }

        #endregion

        /// <summary>
        /// Determine IIS version based on registry
        /// </summary>
        /// <returns></returns>
        private Version GetIISVersion()
        {
            using (var componentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
            {
                if (componentsKey != null)
                {
                    var majorVersion = (int)componentsKey.GetValue("MajorVersion", -1);
                    var minorVersion = (int)componentsKey.GetValue("MinorVersion", -1);
                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        return new Version(majorVersion, minorVersion);
                    }
                }
                return new Version(0, 0);
            }
        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_ServerManager != null)
                    {
                        _ServerManager.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        #endregion

    }
}