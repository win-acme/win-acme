using Autofac;
using Microsoft.Web.Administration;
using Microsoft.Win32;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    internal class IISClient : IIISClient<IISSiteWrapper, IISBindingWrapper>
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
        public void Commit()
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
                    Where(s => {
                        try {
                            return s.State == ObjectState.Started;
                        } catch {
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
                return Version.Major >= 8 && FtpSites.Count() > 0;
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

        /// <summary>
        /// Update/create bindings for all host names in the certificate
        /// </summary>
        /// <param name="target"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        public void AddOrUpdateBindings(IEnumerable<string> identifiers, BindingOptions bindingOptions, byte[] oldThumbprint)
        {
            try
            {
                IEnumerable<(IISSiteWrapper site, Binding binding)> allBindings = WebSites.
                    SelectMany(site => site.Site.Bindings, (site, binding) => (site, binding)).
                    ToList();

                var bindingsUpdated = 0;
                var found = new List<string>();
                if (oldThumbprint != null)
                {
                    var siteBindings = allBindings.
                        Where(sb => StructuralComparisons.StructuralEqualityComparer.Equals(sb.binding.CertificateHash, oldThumbprint)).
                        ToList();

                    // Update all bindings created using the previous certificate
                    foreach (var (site, binding) in siteBindings)
                    {
                        try
                        {
                            UpdateBinding(site.Site, binding, bindingOptions);
                            found.Add(binding.Host);
                            bindingsUpdated += 1;
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex, "Error updating binding {host}", binding.BindingInformation);
                            throw;
                        }
                    }
                }

                // Find all hostnames which are not covered by any of the already updated
                // bindings yet, because we will want to make sure that those are accessable
                // in the target site
                var targetSite = GetWebSite(bindingOptions.SiteId ?? -1);
                IEnumerable<string> todo = identifiers;
                while (todo.Any())
                {
                    // Filter by previously matched bindings
                    todo = todo.Where(cert => !found.Any(iis => Fits(iis, cert, bindingOptions.Flags) > 0));
                    if (!todo.Any()) break;

                    var current = todo.First();
                    try
                    {
                        var binding = AddOrUpdateBindings(
                            allBindings.Select(x => x.binding).ToArray(),
                            targetSite,
                            bindingOptions.WithHost(current),
                            !bindingOptions.Flags.HasFlag(SSLFlags.CentralSSL));
                         
                        // Allow a single newly created binding to match with 
                        // multiple hostnames on the todo list, e.g. the *.example.com binding
                        // matches with both a.example.com and b.example.com
                        if (binding == null)
                        {
                            // We were unable to create the binding because it would
                            // lead to a duplicate. Pretend that we did add it to 
                            // still be able to get out of the loop;
                            found.Add(current);
                        }
                        else
                        {
                            found.Add(binding);
                            bindingsUpdated += 1;
                        }
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

                if (bindingsUpdated > 0)
                {
                    _log.Information("Committing {count} {type} binding changes to IIS", bindingsUpdated, "https");
                    Commit();
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
        /// <param name="ipAddress"></param>
        /// <param name="fuzzy"></param>
        private string AddOrUpdateBindings(Binding[] allBindings, IISSiteWrapper site, BindingOptions bindingOptions, bool fuzzy)
        {
            // Get all bindings which could map to the host
            var matchingBindings = site.Site.Bindings.
                Select(x => new { binding = x, fit = Fits(x.Host, bindingOptions.Host, bindingOptions.Flags) }).
                Where(x => x.fit > 0).
                OrderByDescending(x => x.fit).
                ToList();

            var httpsMatches = matchingBindings.Where(x => x.binding.Protocol == "https");
            var httpMatches = matchingBindings.Where(x => x.binding.Protocol == "http");

            // Existing https binding for exactly the domain we are looking for, will be
            // updated to use the new ACME certificate
            var perfectHttpsMatches = httpsMatches.Where(x => x.fit == 100);
            if (perfectHttpsMatches.Any())
            {
                foreach (var perfectMatch in perfectHttpsMatches)
                {
                    var updateOptions = bindingOptions.WithFlags(UpdateFlags(bindingOptions.Flags, perfectMatch.binding, allBindings));
                    UpdateBinding(site.Site, perfectMatch.binding, updateOptions);
                }
                return bindingOptions.Host;
            }

            // If we find a http-binding for the domain, a corresponding https binding
            // is set up to match incoming secure traffic
            var perfectHttpMatches = httpMatches.Where(x => x.fit == 100);
            if (perfectHttpMatches.Any())
            {
                if (AllowAdd(bindingOptions, allBindings))
                {
                    AddBinding(site, bindingOptions);
                    return bindingOptions.Host;
                } 
            }

            // Allow partial matching. Doesn't work for IIS CCS. Also 
            // should not be used for TLS-SNI validation.
            if (bindingOptions.Host.StartsWith("*.") || fuzzy)
            {
                httpsMatches = httpsMatches.Except(perfectHttpsMatches);
                httpMatches = httpMatches.Except(perfectHttpMatches);

                // There are no perfect matches for the domain, so at this point we start
                // to look at wildcard and/or default bindings binding. Since they are 
                // order by 'best fit' we look at the first one.
                if (httpsMatches.Any())
                {
                    var bestMatch = httpsMatches.First();
                    var updateFlags = UpdateFlags(bindingOptions.Flags, bestMatch.binding, allBindings);
                    var updateOptions = bindingOptions.WithFlags(updateFlags);
                    UpdateBinding(site.Site, bestMatch.binding, updateOptions);
                    return bestMatch.binding.Host;
                }

                // Nothing on https, then start to look at http
                if (httpMatches.Any())
                {
                    var bestMatch = httpMatches.First();
                    var addOptions = bindingOptions.WithHost(bestMatch.binding.Host);
                    if (AllowAdd(addOptions, allBindings))
                    {
                        AddBinding(site, addOptions);
                        return bestMatch.binding.Host;
                    }
                }
            }


            // At this point we haven't even found a partial match for our hostname
            // so as the ultimate step we create new https binding
            if (AllowAdd(bindingOptions, allBindings))
            {
                AddBinding(site, bindingOptions);
                return bindingOptions.Host;
            }
            return null;
        }

        /// <summary>
        /// Turn on SNI for #915
        /// </summary>
        /// <param name="start"></param>
        /// <param name="match"></param>
        /// <param name="allBindings"></param>
        /// <returns></returns>
        private bool AllowAdd(BindingOptions options, Binding[] allBindings)
        {
            var bindingInfo = $"{options.IP}:{options.Port}:{options.Host}";
            var ret = !allBindings.Any(x => x.BindingInformation == bindingInfo);
            if (!ret)
            {
                _log.Warning($"Prevent adding duplicate binding for {bindingInfo}");
            }
            return ret;
        }

        /// <summary>
        /// Turn on SNI for #915
        /// </summary>
        /// <param name="start"></param>
        /// <param name="match"></param>
        /// <param name="allBindings"></param>
        /// <returns></returns>
        private SSLFlags UpdateFlags(SSLFlags start, Binding match, Binding[] allBindings)
        {
            var updateFlags = start;
            if (Version.Major >= 8 && !match.HasSSLFlags(SSLFlags.SNI))
            {
                if (allBindings
                    .Except(new[] { match })
                    .Where(x => StructuralComparisons.StructuralEqualityComparer.Equals(match.CertificateHash, x.CertificateHash))
                    .Where(x => !x.HasSSLFlags(SSLFlags.SNI))
                    .Any())
                {
                    _log.Warning("Turning on SNI for existing binding to avoid conflict");
                    return start | SSLFlags.SNI;
                }
            }
            return start;
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
        private void AddBinding(IISSiteWrapper site, BindingOptions options)
        {
            options = options.WithFlags(CheckFlags(options.Host, options.Flags));
            _log.Information(true, "Adding new https binding {binding}", options.Binding);
            var newBinding = site.Site.Bindings.CreateElement("binding");
            newBinding.Protocol = "https";
            newBinding.BindingInformation = options.Binding;
            newBinding.CertificateStoreName = options.Store;
            newBinding.CertificateHash = options.Thumbprint;
            if (!string.IsNullOrEmpty(options.Host) && Version.Major >= 8)
            {
                options = options.WithFlags(options.Flags | SSLFlags.SNI);
            }
            if (options.Flags > 0)
            {
                newBinding.SetAttributeValue("sslFlags", options.Flags);
            }
            site.Site.Bindings.Add(newBinding);
        }

        /// <summary>
        /// Test if the host fits to the binding
        /// 100: full match
        /// 90: partial match (Certificate less specific, e.g. *.example.com cert for sub.example.com binding)
        /// 50,59,48,...: partial match (IIS less specific, e.g. sub.example.com cert for *.example.com binding)
        /// 10: default match (catch-all binding)
        /// 0: no match
        /// </summary>
        /// <param name=""></param>
        /// <param name=""></param>
        /// <returns></returns>
        private int Fits(string iis, string certificate, SSLFlags flags)
        {
            // The default (emtpy) binding matches with all hostnames.
            // But it's not supported with Central SSL
            if (string.IsNullOrEmpty(iis) && (!flags.HasFlag(SSLFlags.CentralSSL)))
            {
                return 10;
            }

            // Match sub.example.com (certificate) with *.example.com (IIS)
            if (iis.StartsWith("*.") && !certificate.StartsWith("*."))
            {
                if (certificate.ToLower().EndsWith(iis.ToLower().Replace("*.", ".")))
                {
                    // If there is a binding for *.a.b.c.com (5) and one for *.c.com (3)
                    // then the hostname test.a.b.c.com (5) is a better (more specific)
                    // for the former than for the latter, so we prefer to use that.
                    var hostLevel = certificate.Split('.').Count();
                    var bindingLevel = iis.Split('.').Count();
                    return 50 - (hostLevel - bindingLevel);
                }
                else
                {
                    return 0;
                }
            }

            // Match *.example.com (certificate) with sub.example.com (IIS)
            if (!iis.StartsWith("*.") && certificate.StartsWith("*."))
            {
                if (iis.ToLower().EndsWith(certificate.ToLower().Replace("*.", ".")))
                {
                    // But it should not match with another.sub.example.com.
                    var hostLevel = certificate.Split('.').Count();
                    var bindingLevel = iis.Split('.').Count();
                    if (hostLevel == bindingLevel)
                    {
                        return 90;
                    }
                }
                else
                {
                    return 0;
                }
            }

            // Full match
            return string.Equals(iis, certificate, StringComparison.CurrentCultureIgnoreCase) ? 100 : 0;
        }

        /// <summary>
        /// Update existing bindng
        /// </summary>
        /// <param name="site"></param>
        /// <param name="existingBinding"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        private void UpdateBinding(Site site, Binding existingBinding, BindingOptions options)
        {
            // Check flags
            options = options.WithFlags(CheckFlags(existingBinding.Host, options.Flags));

            // IIS 7.x is very picky about accessing the sslFlags attribute
            var currentFlags = existingBinding.SSLFlags();
            if ((currentFlags & ~SSLFlags.SNI) == (options.Flags & ~SSLFlags.SNI) && // Don't care about SNI status
                ((options.Store == null && existingBinding.CertificateStoreName == null) ||
                StructuralComparisons.StructuralEqualityComparer.Equals(existingBinding.CertificateHash, options.Thumbprint) &&
                string.Equals(existingBinding.CertificateStoreName, options.Store, StringComparison.InvariantCultureIgnoreCase)))
            {
                _log.Verbose("No binding update needed");
            }
            else
            {
                _log.Information(true, "Updating existing https binding {host}:{port}", existingBinding.Host, existingBinding.EndPoint.Port);

                // Replace instead of change binding because of #371
                var handled = new[] {
                    "protocol",
                    "bindingInformation",
                    "sslFlags",
                    "certificateStoreName",
                    "certificateHash"
                };
                var replacement = site.Bindings.CreateElement("binding");
                replacement.Protocol = existingBinding.Protocol;
                replacement.BindingInformation = existingBinding.BindingInformation;
                replacement.CertificateStoreName = options.Store;
                replacement.CertificateHash = options.Thumbprint;
                foreach (var attr in existingBinding.Attributes)
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
                    options = options.WithFlags(options.Flags | SSLFlags.SNI);
                }
                if (options.Flags > 0)
                {
                    replacement.SetAttributeValue("sslFlags", options.Flags);
                }
                site.Bindings.Remove(existingBinding);
                site.Bindings.Add(replacement);
            }
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

    }
}