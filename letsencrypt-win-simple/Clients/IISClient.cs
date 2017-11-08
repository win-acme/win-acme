using Autofac;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Clients
{
    public class IISClient 
    {
        private const string _anonymousAuthenticationSection = "system.webServer/security/authentication/anonymousAuthentication";
        private const string _accessSecuritySection = "system.webServer/security/access";
        private const string _handlerSection = "system.webServer/handlers";
        private const string _ipSecuritySection = "system.webServer/security/ipSecurity";
        private const string _urlRewriteSection = "system.webServer/rewrite/rules";
        private const string _modulesSection = "system.webServer/modules";

        public static Version Version = GetIISVersion();
        private bool RewriteModule = GetRewriteModulePresent();
        public IdnMapping IdnMapping = new IdnMapping();
        protected ILogService _log;
        protected IOptionsService _optionsService;

        public enum SSLFlags
        {
            SNI = 1,
            CentralSSL = 2
        }

        public IISClient()
        {
            _log = Program.Container.Resolve<ILogService>();
            _optionsService = Program.Container.Resolve<IOptionsService>();
        }

        public ServerManager ServerManager
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
        public void Commit()
        {
            if (_ServerManager != null)
            {
                _ServerManager.CommitChanges();
                _ServerManager = null;
            }
        }

        private ServerManager _ServerManager;

        /// <summary>
        /// Configures the site for ACME validation without generating an overly complicated web.config
        /// </summary>
        /// <param name="target"></param>
        public void PrepareSite(Target target)
        {
            var config = ServerManager.GetApplicationHostConfiguration();
            var site = GetSite(target);

            // Only do it for the .well-known folder, do not compromise security for other parts of the application
            var wellKnown = "/.well-known";
            var parentPath = site.Name;
            var path = parentPath + wellKnown;

            // Create application
            var rootApp = site.Applications.FirstOrDefault(x => x.Path == "/");
            var rootVdir = rootApp.VirtualDirectories.FirstOrDefault(x => x.Path == "/");
            var leApp = site.Applications.FirstOrDefault(a => a.Path == wellKnown);
            var leVdir = leApp?.VirtualDirectories.FirstOrDefault(v => v.Path == "/");
            if (leApp == null)
            {
                leApp = site.Applications.CreateElement();
                leApp.ApplicationPoolName = rootApp.ApplicationPoolName;
                leApp.Path = wellKnown;
                site.Applications.Add(leApp);
            }
            if (leVdir == null)
            {
                leVdir = leApp.VirtualDirectories.CreateElement();
                leVdir.Path = "/";
                leApp.VirtualDirectories.Add(leVdir);
            }
            leVdir.PhysicalPath = rootVdir.PhysicalPath.TrimEnd('\\') + "\\.well-known\\";

            // Enabled Anonymous authentication
            ConfigurationSection anonymousAuthenticationSection = config.GetSection(_anonymousAuthenticationSection, path);
            anonymousAuthenticationSection["enabled"] = true;

            // Disable "Require SSL"
            ConfigurationSection accessSecuritySection = config.GetSection(_accessSecuritySection, path);
            accessSecuritySection["sslFlags"] = "None";

            // Disable IP restrictions
            ConfigurationSection ipSecuritySection = config.GetSection(_ipSecuritySection, path);
            ipSecuritySection["allowUnlisted"] = true;

            ConfigurationSection globalModules = config.GetSection(_modulesSection);
            ConfigurationSection parentModules = config.GetSection(_modulesSection, parentPath);
            ConfigurationSection modulesSection = config.GetSection(_modulesSection, path);
            ConfigurationElementCollection modulesCollection = modulesSection.GetCollection();
            modulesSection["runAllManagedModulesForAllRequests"] = false;
            var globals = globalModules.GetCollection().Select(gm => gm.GetAttributeValue("name")).ToList();
            foreach (var module in parentModules.GetCollection())
            {
                var moduleName = module.GetAttributeValue("name");
                if (!globals.Contains(moduleName))
                {
                    ConfigurationElement newModule = modulesCollection.CreateElement("remove");
                    newModule.SetAttributeValue("name", moduleName);
                    modulesCollection.Add(newModule);
                }
            }

            // Configure handlers
            ConfigurationSection handlerSection = config.GetSection(_handlerSection, path);
            ConfigurationElementCollection handlersCollection = handlerSection.GetCollection();
            handlersCollection.Clear();
            ConfigurationElement addElement = handlersCollection.CreateElement("add");
            addElement["modules"] = "StaticFileModule,DirectoryListingModule";
            addElement["name"] = "StaticFile";
            addElement["resourceType"] = "Either";
            addElement["path"] = "*";
            addElement["verb"] = "GET";
            handlersCollection.Add(addElement);

            // Disable URL rewrite
            if (RewriteModule)
            {
                try
                {
                    ConfigurationSection urlRewriteSection = config.GetSection(_urlRewriteSection, path);
                    ConfigurationElementCollection urlRewriteCollection = urlRewriteSection.GetCollection();
                    urlRewriteCollection.Clear();
                }
                catch { }
            }

            // Save
            Commit();
        }

        /// <summary>
        /// Configures the site for ACME validation without generating an overly complicated web.config
        /// </summary>
        /// <param name="target"></param>
        public void UnprepareSite(Target target)
        {
            var config = ServerManager.GetApplicationHostConfiguration();
            var site = GetSite(target);

            // Remove application
            var rootApp = site.Applications.FirstOrDefault(x => x.Path == "/");
            var rootVdir = rootApp.VirtualDirectories.FirstOrDefault(x => x.Path == "/");
            var leApp = site.Applications.FirstOrDefault(a => a.Path == "/.well-known");
            if (leApp != null)
            {
                site.Applications.Remove(leApp);
            }

            // Save
            Commit();
        }

        /// <summary>
        /// Update/create bindings for all host names in the certificate
        /// </summary>
        /// <param name="target"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        public void AddOrUpdateBindings(Target target, SSLFlags flags, CertificateInfo newCertificate, CertificateInfo oldCertificate)
        {
            try
            {
                var targetSite = GetSite(target);
                IEnumerable<string> todo = target.GetHosts(true);
                var found = new List<string>();
                var oldThumbprint = oldCertificate?.Certificate?.GetCertHash();
                if (oldThumbprint != null)
                {
                    var siteBindings = ServerManager.Sites.
                        SelectMany(site => site.Bindings, (site, binding) => new { site, binding }).
                        Where(sb => sb.binding.Protocol == "https").
                        Where(sb => sb.site.State == ObjectState.Started). // Prevent errors with duplicate bindings
                        Where(sb => sb.site.Id != targetSite.Id).
                        Where(sb => StructuralComparisons.StructuralEqualityComparer.Equals(sb.binding.CertificateHash, oldThumbprint)).
                        ToList();

                    // Out-of-target bindings created using the old certificate, so let's 
                    // assume the user wants to update them and not create new bindings in
                    // the actual target site.
                    foreach (var sb in siteBindings)
                    {
                        try
                        {
                            UpdateBinding(sb.site, 
                                sb.binding, 
                                flags, 
                                newCertificate.Certificate.GetCertHash(), 
                                newCertificate.Store?.Name);
                            found.Add(sb.binding.Host.ToLower());
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error updating binding {host}", sb.binding.BindingInformation);
                            throw;
                        }
                    }
                }

                // We are left with bindings that have no https equivalent in any site yet
                // so we will create them in the orginal target site
                foreach (var host in todo)
                {
                    try
                    {
                        AddOrUpdateBindings(targetSite, 
                            host, 
                            flags, 
                            newCertificate.Certificate.GetCertHash(), 
                            newCertificate.Store?.Name, 
                            _optionsService.Options.SSLPort, 
                            !found.Contains(host));
                    }
                    catch (Exception ex)
                    {
                        _log.Error(ex, "Error creating binding {host}: {ex}", host, ex.Message);
                        throw;
                    }
                }
                _log.Information("Committing binding changes to IIS");
                Commit();
                _log.Information("IIS will serve the new certificates after the Application Pool IdleTimeout has been reached.");
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Error installing");
                throw;
            }
        }

        /// <summary>
        /// Create or update a single binding in a single site
        /// </summary>
        /// <param name="site"></param>
        /// <param name="host"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        /// <param name="newPort"></param>
        public void AddOrUpdateBindings(Site site, string host, SSLFlags flags, byte[] thumbprint, string store, int newPort = 443, bool allowCreate = true)
        {
            var existingBindings = site.Bindings.Where(x => string.Equals(x.Host, host, StringComparison.CurrentCultureIgnoreCase)).ToList();
            var existingHttpsBindings = existingBindings.Where(x => x.Protocol == "https").ToList();
            var existingHttpBindings = existingBindings.Where(x => x.Protocol == "http").ToList();
            var update = existingHttpsBindings.Any();
            if (update)
            {
                // Already on HTTPS, update those bindings to use the Let's Encrypt
                // certificate instead of the existing one. Note that this only happens
                // for the target website, if other websites have bindings using other
                // certificates, they will remain linked to the old ones.
                foreach (var existingBinding in existingHttpsBindings)
                {
                    UpdateBinding(site, existingBinding, flags, thumbprint, store);
                }
            }
            else if (allowCreate)
            {
                _log.Information(true, "Adding new https binding {host}:{port}", host, newPort);
                string IP = "*";
                if (existingHttpBindings.Any())
                {
                    IP = GetIP(existingHttpBindings.First().EndPoint.ToString(), host);
                }
                else
                {
                    _log.Warning("No HTTP binding for {host} on {name}", host, site.Name);
                }
                Binding newBinding = site.Bindings.CreateElement("binding");
                newBinding.Protocol = "https";
                newBinding.BindingInformation = $"{IP}:{newPort}:{host}";
                newBinding.CertificateStoreName = store;
                newBinding.CertificateHash = thumbprint;
                if (flags > 0)
                {
                    newBinding.SetAttributeValue("sslFlags", flags);
                }
                site.Bindings.Add(newBinding);
            }
            else
            {
                _log.Information("Binding not created");
            }
        }

        /// <summary>
        /// Update existing bindng
        /// </summary>
        /// <param name="site"></param>
        /// <param name="existingBinding"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        private void UpdateBinding(Site site, Binding existingBinding, SSLFlags flags, byte[] thumbprint, string store)
        {
            // IIS 7.x is very picky about accessing the sslFlags attribute
            var currentFlags = existingBinding.Attributes.
                    Where(x => x.Name == "sslFlags").
                    Where(x => x.Value != null).
                    Select(x => int.Parse(x.Value.ToString())).
                    FirstOrDefault();

            if (currentFlags == (int)flags &&
                StructuralComparisons.StructuralEqualityComparer.Equals(existingBinding.CertificateHash, thumbprint) &&
                string.Equals(existingBinding.CertificateStoreName, store, StringComparison.InvariantCultureIgnoreCase))
            {
                _log.Verbose("No binding update needed");
            }
            else
            {
                _log.Information(true, "Updating existing https binding {host}:{port}", existingBinding.Host, existingBinding.EndPoint.Port);

                // Replace instead of change binding because of #371
                var handled = new[] { "protocol", "bindingInformation", "sslFlags", "certificateStoreName", "certificateHash" };
                Binding replacement = site.Bindings.CreateElement("binding");
                replacement.Protocol = existingBinding.Protocol;
                replacement.BindingInformation = existingBinding.BindingInformation;
                replacement.CertificateStoreName = store;
                replacement.CertificateHash = thumbprint;
                foreach (ConfigurationAttribute attr in existingBinding.Attributes)
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
           
                if (flags > 0 || currentFlags > 0)
                {
                    replacement.SetAttributeValue("sslFlags", flags);
                }
                site.Bindings.Remove(existingBinding);
                site.Bindings.Add(replacement);
            }
        }

        private static Version GetIISVersion()
        {
            using (RegistryKey componentsKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\InetStp", false))
            {
                if (componentsKey != null)
                {
                    int majorVersion = (int)componentsKey.GetValue("MajorVersion", -1);
                    int minorVersion = (int)componentsKey.GetValue("MinorVersion", -1);
                    if (majorVersion != -1 && minorVersion != -1)
                    {
                        return new Version(majorVersion, minorVersion);
                    }
                }
                return new Version(0, 0);
            }
        }

        private static bool GetRewriteModulePresent()
        {
            using (RegistryKey rewriteKey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\IIS Extensions\URL Rewrite", false))
            {
                return rewriteKey != null;
            }
        }

        private Site GetSite(Target target)
        {
            foreach (var site in ServerManager.Sites)
            {
                if (site.Id == target.SiteId) return site;
            }
            throw new Exception($"Unable to find IIS site ID #{target.SiteId} for binding {this}");
        }

        /// <summary>
        /// Use IP of HTTP binding
        /// </summary>
        /// <param name="httpEndpoint"></param>
        /// <param name="host"></param>
        /// <returns></returns>
        private string GetIP(string httpEndpoint, string host)
        {
            string IP = "*";
            string HTTPIP = httpEndpoint.Substring(0, httpEndpoint.IndexOf(':'));
            if (HTTPIP != "0.0.0.0")
            {
                IP = HTTPIP;
            }
            return IP;
        }

        internal Target UpdateWebRoot(Target saved, Target match)
        {
            // Update web root path
            if (!string.Equals(saved.WebRootPath, match.WebRootPath, StringComparison.InvariantCultureIgnoreCase))
            {
                _log.Warning("- Change WebRootPath from {old} to {new}", saved.WebRootPath, match.WebRootPath);
                saved.WebRootPath = match.WebRootPath;
            }
            return saved;
        }

        internal Target UpdateAlternativeNames(Target saved, Target match)
        {
            // Add/remove alternative names
            var addedNames = match.AlternativeNames.Except(saved.AlternativeNames).Except(saved.GetExcludedHosts());
            var removedNames = saved.AlternativeNames.Except(match.AlternativeNames);
            if (addedNames.Count() > 0)
            {
                _log.Warning("- Detected new host(s) {names} in {target}", string.Join(", ", addedNames), saved.Host);
            }
            if (removedNames.Count() > 0)
            {
                _log.Warning("- Detected missing host(s) {names} in {target}", string.Join(", ", removedNames), saved.Host);
            }
            saved.AlternativeNames = match.AlternativeNames;
            return saved;
        }
    }
}