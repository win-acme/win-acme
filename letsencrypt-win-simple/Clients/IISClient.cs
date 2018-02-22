using Autofac;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PKISharp.WACS.Clients
{
    public class IISClient
    {
        private const string _anonymousAuthenticationSection = "system.webServer/security/authentication/anonymousAuthentication";
        private const string _accessSecuritySection = "system.webServer/security/access";
        private const string _handlerSection = "system.webServer/handlers";
        private const string _ipSecuritySection = "system.webServer/security/ipSecurity";
        private const string _urlRewriteSection = "system.webServer/rewrite/rules";
        private const string _modulesSection = "system.webServer/modules";

        public Version Version = GetIISVersion();
        private bool RewriteModule = GetRewriteModulePresent();
        public IdnMapping IdnMapping = new IdnMapping();
        protected ILogService _log;

        [Flags]
        public enum SSLFlags
        {
            SNI = 1,
            CentralSSL = 2
        }

        public IISClient(ILogService log)
        {
            _log = log;
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


        public IEnumerable<Site> RunningWebsites()
        {
            return ServerManager.Sites.AsEnumerable().
                Where(s => s.Bindings.Any(sb => sb.Protocol == "http" || sb.Protocol == "https")).
                Where(s => s.State == ObjectState.Started).
                OrderBy(s => s.Name);
        }

        private const string _wellKnown = ".well-known";
        private const string _acmeChallenge = "acme-challenge";

        /// <summary>
        /// Configures the site for ACME validation without generating an overly complicated web.config
        /// </summary>
        /// <param name="target"></param>
        public void PrepareSite(Target target)
        {
            var config = ServerManager.GetApplicationHostConfiguration();
            var siteId = target.ValidationSiteId ?? target.TargetSiteId ?? -1;
            var site = GetSite(siteId);

            // Only do it for the .well-known folder, do not compromise security for other parts of the application
            var wellKnown = $"/{_wellKnown}";
            var acmeChallenge = $"/{_wellKnown}/{_acmeChallenge}";
            var parentPath = site.Name + wellKnown;
            var path = site.Name + acmeChallenge;

            // Create application
            var rootApp = site.Applications.FirstOrDefault(x => x.Path == "/");
            var rootVdir = rootApp.VirtualDirectories.FirstOrDefault(x => x.Path == "/");
            var wkApp = site.Applications.FirstOrDefault(a => a.Path == wellKnown);
            var acApp = site.Applications.FirstOrDefault(a => a.Path == acmeChallenge);
            var wkVdir = wkApp?.VirtualDirectories.FirstOrDefault(v => v.Path == "/");
            var acVdir = acApp?.VirtualDirectories.FirstOrDefault(v => v.Path == "/");
            if (wkApp == null)
            {
                wkApp = site.Applications.CreateElement();
                wkApp.ApplicationPoolName = rootApp.ApplicationPoolName;
                wkApp.Path = wellKnown;
                site.Applications.Add(wkApp);
            }
            if (acApp == null)
            {
                acApp = site.Applications.CreateElement();
                acApp.ApplicationPoolName = rootApp.ApplicationPoolName;
                acApp.Path = acmeChallenge;
                site.Applications.Add(acApp);
            }
            if (wkVdir == null)
            {
                wkVdir = wkApp.VirtualDirectories.CreateElement();
                wkVdir.Path = "/";
                wkApp.VirtualDirectories.Add(wkVdir);
                wkVdir.PhysicalPath = $"{rootVdir.PhysicalPath.TrimEnd('\\')}\\{_wellKnown}\\";
            }
            if (acVdir == null)
            {
                acVdir = wkApp.VirtualDirectories.CreateElement();
                acVdir.Path = "/";
                acApp.VirtualDirectories.Add(acVdir);
            }
            acVdir.PhysicalPath = $"{rootVdir.PhysicalPath.TrimEnd('\\')}\\{_wellKnown}\\{_acmeChallenge}\\";

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
            var globals = globalModules.GetCollection().Select(gm => gm.GetAttributeValue("name")).ToList();

            var local = ServerManager.GetWebConfiguration(site.Name, path);
            ConfigurationSection localModules = local.GetSection(_modulesSection);

            ConfigurationSection modulesSection = config.GetSection(_modulesSection, path);
            ConfigurationElementCollection modulesCollection = modulesSection.GetCollection();
            modulesSection["runAllManagedModulesForAllRequests"] = false;
            foreach (var module in localModules.GetCollection())
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
            var siteId = target.ValidationSiteId ?? target.TargetSiteId ?? -1;
            var site = GetSite(siteId);

            // Remove application
            var rootApp = site.Applications.FirstOrDefault(x => x.Path == "/");
            var rootVdir = rootApp.VirtualDirectories.FirstOrDefault(x => x.Path == "/");
            var wellKnown = $"/{_wellKnown}";
            var acmeChallenge = $"/{_wellKnown}/{_acmeChallenge}";

            var wkApp = site.Applications.FirstOrDefault(a => a.Path == wellKnown);
            if (wkApp != null)
            {
                site.Applications.Remove(wkApp);
            }
            var acApp = site.Applications.FirstOrDefault(a => a.Path == acmeChallenge);
            if (acApp != null)
            {
                site.Applications.Remove(acApp);
            }
        
            // Remove specific config
            config.RemoveLocationPath($"{site.Name}/{_wellKnown}/{_acmeChallenge}");

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
                var allBindings = RunningWebsites().
                    SelectMany(site => site.Bindings, (site, binding) => new { site, binding }).
                    ToList();

                var bindingsUpdated = 0;
                var found = new List<string>();
                var oldThumbprint = oldCertificate?.Certificate?.GetCertHash();
                if (oldThumbprint != null)
                {
                    var siteBindings = allBindings.
                        Where(sb => StructuralComparisons.StructuralEqualityComparer.Equals(sb.binding.CertificateHash, oldThumbprint)).
                        ToList();

                    // Update all bindings created using the previous certificate
                    foreach (var sb in siteBindings)
                    {
                        try
                        {
                            UpdateBinding(sb.site, 
                                sb.binding, 
                                flags, 
                                newCertificate.Certificate.GetCertHash(), 
                                newCertificate.Store?.Name);
                            found.Add(sb.binding.Host);
                            bindingsUpdated += 1;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error updating binding {host}", sb.binding.BindingInformation);
                            throw;
                        }
                    }
                }

                // Find all hostnames which are not covered by any of the already updated
                // bindings yet, because we will want to make sure that those are accessable
                // in the target site
                var targetSite = GetSite(target.InstallationSiteId ?? target.TargetSiteId ?? -1);
                IEnumerable<string> todo = target.GetHosts(true);
                while (todo.Count() > 0)
                {
                    // Filter by previously matched bindings
                    todo = todo.Where(host => !found.Any(binding => Fits(binding, host, flags) > 0));
                    if (todo.Count() > 0)
                    {
                        var current = todo.First();
                        try
                        {
                            var binding = AddOrUpdateBindings(
                                            targetSite,
                                            current,
                                            flags,
                                            newCertificate.Certificate.GetCertHash(),
                                            newCertificate.Store?.Name,
                                            target.SSLPort);
                         
                            // Allow a single newly created binding to match with 
                            // multiple hostnames on the todo list, e.g. the *.example.com binding
                            // matches with both a.example.com and b.example.com
                            found.Add(binding);
                            bindingsUpdated += 1;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error creating binding {host}: {ex}", current, ex.Message);

                            // Prevent infinite retry loop, we just skip the domain when
                            // an error happens creating a new binding for it. User can
                            // always change/add the bindings manually after all.
                            found.Add(current);
                        }

                    }
                }

                if (bindingsUpdated > 0)
                {
                    _log.Information("Committing {count} binding changes to IIS", bindingsUpdated);
                    Commit();
                    _log.Information("IIS will serve the new certificates after the Application Pool IdleTimeout has been reached.");
                }
                else
                {
                    _log.Warning("No bindings have been changed");
                }
               
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
        /// <param name="port"></param>
        public string AddOrUpdateBindings(Site site, string host, SSLFlags flags, byte[] thumbprint, string store, int? port)
        {
            // Get all bindings which could map to the host
            var matchingBindings = site.Bindings.
                Select(x => new { binding = x, fit = Fits(x.Host, host, flags) }).
                Where(x => x.fit > 0).
                OrderByDescending(x => x.fit).
                ToList();

            var httpsMatches = matchingBindings.Where(x => x.binding.Protocol == "https");
            var httpMatches = matchingBindings.Where(x => x.binding.Protocol == "http");

            // Existing https binding for exactly the domain we are looking for, will be
            // updated to use the new Let's Encrypt certificate
            var perfectHttpsMatches = httpsMatches.Where(x => x.fit == 100);
            if (perfectHttpsMatches.Any())
            {
                foreach (var perfectMatch in perfectHttpsMatches)
                {
                    UpdateBinding(site, perfectMatch.binding, flags, thumbprint, store);
                }
                return host;
            }

            // If we find a http-binding for the domain, a corresponding https binding
            // is set up to match incoming secure traffic
            var perfectHttpMatches = httpMatches.Where(x => x.fit == 100);
            if (perfectHttpMatches.Any())
            {
                AddBinding(site, host, flags, thumbprint, store, port, "*");
                return host;
            }

            // There are no perfect matches for the domain, so at this point we start
            // to look at wildcard and/or default bindings binding. Since they are 
            // order by 'best fit' we look at the first one.
            if (httpsMatches.Any())
            {
                var bestMatch = httpsMatches.First();
                UpdateBinding(site, bestMatch.binding, flags, thumbprint, store);
                return bestMatch.binding.Host;
            }
            
            // Nothing on https, then start to look at http
            if (httpMatches.Any())
            {
                var bestMatch = httpMatches.First();
                AddBinding(site, bestMatch.binding.Host, flags, thumbprint, store, port, "*");
                return bestMatch.binding.Host;
            }

            // At this point we haven't even found a partial match for our hostname
            // so as the ultimate step we create new https binding
            AddBinding(site, host, flags, thumbprint, store, port, "*");
            return host;
        }

        /// <summary>
        /// Make sure the flags are set correctly for updating the binding,
        /// because special conditions apply to the default binding
        /// </summary>
        /// <param name="host"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private SSLFlags CheckFlags(string host, SSLFlags flags)
        {
            // Remove SNI flag from empty binding
            if (string.IsNullOrEmpty(host))
            {
                if (flags.HasFlag(SSLFlags.CentralSSL))
                {
                    throw new InvalidOperationException("Central SSL is not supported without a hostname");
                }
            }
            return flags;
        }

        /// <summary>
        /// Create a new binding
        /// </summary>
        /// <param name="site"></param>
        /// <param name="host"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        /// <param name="port"></param>
        /// <param name="IP"></param>
        private void AddBinding(Site site, string host, SSLFlags flags, byte[] thumbprint, string store, int? port, string IP)
        {
            flags = CheckFlags(host, flags);
            var finalPort = ((port ?? 0) == 0) ? 443 : port;
            _log.Information(true, "Adding new https binding {host}:{port}", host, finalPort);
            Binding newBinding = site.Bindings.CreateElement("binding");
            newBinding.Protocol = "https";
            newBinding.BindingInformation = $"{IP}:{finalPort}:{host}";
            newBinding.CertificateStoreName = store;
            newBinding.CertificateHash = thumbprint;
            if (!string.IsNullOrEmpty(host) && Version.Major >= 8)
            {
                flags |= SSLFlags.SNI;
            }
            if (flags > 0)
            {
                newBinding.SetAttributeValue("sslFlags", flags);
            }
            site.Bindings.Add(newBinding);
        }

        /// <summary>
        /// Test if the host fits to the binding
        /// 0: no match
        /// 100: default match
        /// 500: partial match (todo: make different levels for # of subdomains 
        /// 1000: full match
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        private int Fits(string binding, string host, SSLFlags flags)
        {
            // The default (emtpy) binding matches with all hostnames.
            // But it's not supported with Central SSL
            if (string.IsNullOrEmpty(binding) && (!flags.HasFlag(SSLFlags.CentralSSL)))
            {
                return 10;
            }

            // Match sub.example.com with *.example.com
            if (binding.StartsWith("*."))
            {
                if (host.ToLower().EndsWith(binding.ToLower().Replace("*.", ".")))
                {
                    // If there is a binding for *.a.b.c.com (5) and one for *.c.com (3)
                    // then the hostname test.a.b.c.com (5) is a better (more specific)
                    // for the former than for the latter, so we prefer to use that.
                    var hostLevel = host.Split('.').Count();
                    var bindingLevel = binding.Split('.').Count();
                    return 90 - (hostLevel - bindingLevel);
                }
                else
                {
                    return 0;
                }
            }
 
            // Full match
            return string.Equals(binding, host, StringComparison.CurrentCultureIgnoreCase) ? 100 : 0;
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
            flags = CheckFlags(existingBinding.Host, flags);

            // IIS 7.x is very picky about accessing the sslFlags attribute
            var currentFlags = (SSLFlags)existingBinding.Attributes.
                    Where(x => x.Name == "sslFlags").
                    Where(x => x.Value != null).
                    Select(x => int.Parse(x.Value.ToString())).
                    FirstOrDefault();

            if ((currentFlags & ~SSLFlags.SNI) == (flags & ~SSLFlags.SNI) && // Don't care about SNI status
                ((store == null && existingBinding.CertificateStoreName == null) ||
                StructuralComparisons.StructuralEqualityComparer.Equals(existingBinding.CertificateHash, thumbprint) &&
                string.Equals(existingBinding.CertificateStoreName, store, StringComparison.InvariantCultureIgnoreCase)))
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

                // If current binding has SNI, the updated version 
                // will also have that flag set, regardless
                // of whether or not it was requested by the caller.
                // Callers should not generally request SNI unless 
                // required for the binding, e.g. for TLS-SNI validation.
                // Otherwise let the admin be in control.
                if (currentFlags.HasFlag(SSLFlags.SNI))
                {
                    flags |= SSLFlags.SNI;
                }
                if (flags > 0)
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

        public Site GetSite(long id)
        {
            foreach (var site in RunningWebsites())
            {
                if (site.Id == id) return site;
            }
            throw new Exception($"Unable to find IIS SiteId #{id}");
        }

        internal Target UpdateWebRoot(Target saved, Site match)
        {
            // Update web root path
            var newPath = match.WebRoot();
            if (!string.Equals(saved.WebRootPath, newPath, StringComparison.InvariantCultureIgnoreCase))
            {
                _log.Warning("- Change WebRootPath from {old} to {new}", saved.WebRootPath, newPath);
                saved.WebRootPath = newPath;
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