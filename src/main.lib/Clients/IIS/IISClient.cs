using Autofac;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;

namespace PKISharp.WACS.Clients.IIS
{
    internal class IISClient : IIISClient<IISSiteWrapper, IISBindingWrapper>, IDisposable
    {
        public const string DefaultBindingPortFormat = "443"; 
        public const int DefaultBindingPort = 443;
        public const string DefaultBindingIp = "*";

        public Version Version { get; set; }

        private readonly ILogService _log;
        private ServerManager? _serverManager;
        private List<IISSiteWrapper>? _sites = null;

        public IISClient(ILogService log, AdminService adminService)
        {
            _log = log;
            Version = GetIISVersion(adminService);
        }

        /// <summary>
        /// Single reference to the ServerManager
        /// </summary>
        private ServerManager? ServerManager
        {
            get
            {
                if (_serverManager == null)
                {
                    if (Version.Major > 0)
                    {
                        try
                        {
                            _serverManager = new ServerManager();
                        } 
                        catch
                        {
                            _log.Error($"Unable to create an IIS ServerManager");
                        }
                        _sites = null;
                    }
                }
                return _serverManager;
            }
        }

        /// <summary>
        /// Commit changes to server manager and remove the 
        /// reference to the cached version because it might
        /// be the cause of some bug to keep using the same
        /// ServerManager to commit multiple changes
        /// </summary>
        private void Commit()
        {
            if (_serverManager != null)
            {
                try
                {
                    _serverManager.CommitChanges();
                }
                catch
                {
                    // We will still set ServerManager to null
                    // so that at least a new one will be created
                    // for the next time
                    Refresh();
                    throw;
                }
                Refresh();
            }
        }

        public void Refresh()
        {
            _sites = null;
            if (_serverManager != null)
            {
                _serverManager.Dispose();
                _serverManager = null;
            }
        }

        #region _ Basic retrieval _

        IEnumerable<IIISSite> IIISClient.Sites => Sites;
        IIISSite IIISClient.GetSite(long id, IISSiteType? type) => GetSite(id, type);
        public bool HasWebSites => Version.Major > 0 && Sites.Any(w => w.Type == IISSiteType.Web);
        public bool HasFtpSites => Version >= new Version(7, 5) && Sites.Any(w => w.Type == IISSiteType.Ftp);

        public IEnumerable<IISSiteWrapper> Sites
        {
            get
            {
                if (ServerManager == null)
                {
                    return new List<IISSiteWrapper>();
                }
                if (_sites == null)
                {
                   _sites = ServerManager.Sites.AsEnumerable().
                       Select(x => new IISSiteWrapper(x)).
                       Where(s =>
                       {
                           switch (s.Type)
                           {
                               case IISSiteType.Ftp:
                                   return true;
                               case IISSiteType.Web:
                                   try
                                   {
                                       return s.Site.State == ObjectState.Started;
                                   }
                                   catch
                                   {
                                       // Prevent COMExceptions such as misconfigured
                                       // application pools from crashing the whole 
                                       _log.Warning("Unable to determine state for Site {id}", s.Id);
                                       return false;
                                   }
                               default:
                                   return false;
                           }
                       }).
                       OrderBy(s => s.Name).
                       ToList();
                }
                return _sites;
            }
        }

        public IISSiteWrapper GetSite(long id, IISSiteType? type)
        {
            var ret = Sites.Where(s => s.Site.Id == id).FirstOrDefault();
            if (ret == null)
            {
                throw new Exception($"Unable to find IIS SiteId #{id}");
            }
            if (type != null && ret.Type != type)
            {
                throw new Exception($"IIS SiteId #{id} is not of the expected type {type}");
            }
            return ret;
        }

        #endregion

        #region _ Https Install _

        public void UpdateHttpSite(IEnumerable<Identifier> identifiers, BindingOptions bindingOptions, byte[]? oldCertificate, IEnumerable<Identifier>? allIdentifiers)
        {
            var updater = new IISHttpBindingUpdater<IISSiteWrapper, IISBindingWrapper>(this, _log);
            var updated = updater.AddOrUpdateBindings(identifiers, bindingOptions, allIdentifiers, oldCertificate);
            if (updated > 0)
            {
                _log.Information("Committing {count} {type} binding changes to IIS while updating site {site}", updated, "https", bindingOptions.SiteId);
                Commit();
            }
            else
            {
                _log.Information("No bindings have been changed while updating site {site}", bindingOptions.SiteId);
            }
        }

        public IIISBinding AddBinding(IISSiteWrapper site, BindingOptions options)
        {
            var newBinding = site.Site.Bindings.CreateElement("binding");
            newBinding.BindingInformation = options.Binding;
            newBinding.CertificateStoreName = options.Store;
            newBinding.CertificateHash = options.Thumbprint?.ToArray();
            newBinding.Protocol = "https";
            if (options.Flags > 0)
            {
                newBinding.SetAttributeValue("sslFlags", options.Flags);
            }
            site.Site.Bindings.Add(newBinding);
            return new IISBindingWrapper(newBinding, true);
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
            replacement.BindingInformation = existingBinding.BindingInformation;
            replacement.CertificateStoreName = options.Store;
            replacement.CertificateHash = options.Thumbprint?.ToArray();
            replacement.Protocol = existingBinding.Protocol;
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
        /// <param name="id"></param>
        /// <param name="newCertificate"></param>
        /// <param name="oldCertificate"></param>
        public void UpdateFtpSite(long? id, CertificateInfo newCertificate, CertificateInfo? oldCertificate)
        {
            var ftpSites = Sites.Where(x => x.Type == IISSiteType.Ftp).ToList();
            var oldThumbprint = oldCertificate?.Certificate?.Thumbprint;
            var newThumbprint = newCertificate?.Certificate?.Thumbprint;
            var newStore = newCertificate?.StoreInfo[typeof(CertificateStore)].Path;
            var updated = 0;

            if (ServerManager == null)
            {
                return;
            }

            var sslElement = ServerManager.SiteDefaults.
                GetChildElement("ftpServer").
                GetChildElement("security").
                GetChildElement("ssl");
            if (RequireUpdate(sslElement, false, oldThumbprint, newThumbprint, newStore))
            {
                sslElement.SetAttributeValue("serverCertHash", newThumbprint);
                sslElement.SetAttributeValue("serverCertStoreName", newStore);
                _log.Information(LogType.All, "Updating default ftp site setting");
                updated += 1;
            } 
            else
            {
                _log.Debug("No update needed for default ftp site settings");
            }

            foreach (var ftpSite in ftpSites)
            {
                sslElement = ftpSite.Site.
                    GetChildElement("ftpServer").
                    GetChildElement("security").
                    GetChildElement("ssl");

                if (RequireUpdate(sslElement, ftpSite.Id == id, oldThumbprint, newThumbprint, newStore))
                {
                    sslElement.SetAttributeValue("serverCertHash", newThumbprint);
                    sslElement.SetAttributeValue("serverCertStoreName", newStore);
                    _log.Information(LogType.All, "Updating ftp site {name}", ftpSite.Site.Name);
                    updated += 1;
                }
                else
                {
                    _log.Debug("No update needed for ftp site {name}", ftpSite.Site.Name);
                }
            }

            if (updated > 0)
            {
                _log.Information("Committing {count} {type} site changes to IIS", updated, "ftp");
                Commit();
            }
        }

        /// <summary>
        /// Test if FTP site needs a binding update
        /// </summary>
        /// <param name="element"></param>
        /// <param name="installSite"></param>
        /// <param name="oldThumbprint"></param>
        /// <param name="newThumbprint"></param>
        /// <param name="newStore"></param>
        /// <returns></returns>
        private bool RequireUpdate(ConfigurationElement element, 
            bool installSite, 
            string? oldThumbprint, string? newThumbprint,
            string? newStore)
        {
            if (string.Equals(oldThumbprint, newThumbprint, StringComparison.CurrentCultureIgnoreCase))
            {
                return false;
            }
            var currentThumbprint = element.GetAttributeValue("serverCertHash").ToString();
            var currentStore = element.GetAttributeValue("serverCertStoreName").ToString();
            bool update;
            if (installSite)
            {
                update =
                    !string.Equals(currentThumbprint, newThumbprint, StringComparison.CurrentCultureIgnoreCase) ||
                    !string.Equals(currentStore, newStore, StringComparison.CurrentCultureIgnoreCase);
            }
            else
            {
                update = string.Equals(currentThumbprint, oldThumbprint, StringComparison.CurrentCultureIgnoreCase);
            }
            return update;
        }

        #endregion

        /// <summary>
        /// Determine IIS version based on registry
        /// </summary>
        /// <returns></returns>
        private Version GetIISVersion(AdminService adminService)
        {
            // Get the W3SVC service
            try
            {
                var anyService = false;
                var allServices = ServiceController.GetServices();
                var w3Service = allServices.FirstOrDefault(s => string.Equals(s.ServiceName, "W3SVC", StringComparison.OrdinalIgnoreCase));
                if (w3Service == null)
                {
                    _log.Verbose("No W3SVC detected");
                }
                else if (w3Service.Status != ServiceControllerStatus.Running)
                {
                    _log.Verbose("W3SVC not running");
                } 
                else
                {
                    _log.Verbose("W3SVC detected and running");
                    anyService = true;
                }
                var ftpService = allServices.FirstOrDefault(s => string.Equals(s.ServiceName, "FTPSVC", StringComparison.OrdinalIgnoreCase));
                if (ftpService == null)
                {
                    _log.Verbose("No FTPSVC detected");
                }
                else if (ftpService.Status != ServiceControllerStatus.Running)
                {
                    _log.Verbose("FTPSVC not running");
                }
                else
                {
                    _log.Verbose("FTPSVC detected and running");
                    anyService = true;
                }
                if (!anyService)
                {
                    return new Version(0, 0);
                }
            }
            catch
            {
                _log.Warning("Unable to scan for services");
            }
            try
            {
                // Try to create a ServerManager object and read from it
                if (adminService.IsAdmin)
                {
                    using var x = new ServerManager();
                    _ = x.ApplicationDefaults;
                }
            }
            catch (Exception ex)
            {
                // Assume no IIS if we're not able to use the server manager
                _log.Verbose("IIS ServerManager exception: {message}", ex.Message);
                return new Version(0, 0);
            }

            // Looks like IIS is working, now lets see if we can determine the version
            try
            {
                using var componentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false);
                if (componentsKey != null)
                {
                    _ = int.TryParse(componentsKey.GetValue("MajorVersion", "-1")?.ToString() ?? "-1", out var majorVersion);
                    _ = int.TryParse(componentsKey.GetValue("MinorVersion", "-1")?.ToString() ?? "-1", out var minorVersion);
                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        return new Version(majorVersion, minorVersion);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Verbose("Error reading IIS version fomr registry: {message}", ex.Message);
            }
            _log.Verbose("Unable to detect IIS version, making assumption");
            return new Version(10, 0);

        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_serverManager != null)
                    {
                        _serverManager.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion

    }
}