using PKISharp.WACS.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    /// <summary>
    /// Modifies IIS bindings
    /// </summary>
    internal class IISHttpBindingUpdater<TSite, TBinding>
        where TSite : IIISSite<TBinding>
        where TBinding : IIISBinding
    {
        private readonly IIISClient<TSite, TBinding> _client;
        private readonly ILogService _log;

        /// <summary>
        /// Constructore
        /// </summary>
        /// <param name="client"></param>
        public IISHttpBindingUpdater(
            IIISClient<TSite, TBinding> client,
            ILogService log)
        {
            _client = client;
            _log = log;
        }

        /// <summary>
        /// Update/create bindings for all host names in the certificate
        /// </summary>
        /// <param name="target"></param>
        /// <param name="flags"></param>
        /// <param name="thumbprint"></param>
        /// <param name="store"></param>
        public int AddOrUpdateBindings(
            IEnumerable<string> identifiers,
            BindingOptions bindingOptions,
            byte[] oldThumbprint)
        {
            // Helper function to get updated sites
            IEnumerable<(TSite site, TBinding binding)> GetAllSites() => _client.WebSites.
                SelectMany(site => site.Bindings, (site, binding) => (site, binding)).
                ToList();

            try
            {
                var allBindings = GetAllSites();
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
                            UpdateBinding(site, binding, bindingOptions);
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
                var targetSite = _client.GetWebSite(bindingOptions.SiteId ?? -1);
                var todo = identifiers;
                while (todo.Any())
                {
                    // Filter by previously matched bindings
                    todo = todo.Where(cert => !found.Any(iis => Fits(iis, cert, bindingOptions.Flags) > 0));
                    if (!todo.Any())
                    {
                        break;
                    }

                    allBindings = GetAllSites();
                    var current = todo.First();
                    try
                    {
                        var binding = AddOrUpdateBindings(
                            allBindings.Select(x => x.binding).ToArray(),
                            targetSite,
                            bindingOptions.WithHost(current));

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
                return bindingsUpdated;
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
        private string? AddOrUpdateBindings(TBinding[] allBindings, TSite site, BindingOptions bindingOptions)
        {
            // Get all bindings which could map to the host
            var matchingBindings = site.Bindings.
                Select(x => new { binding = x, fit = Fits(x.Host, bindingOptions.Host, bindingOptions.Flags) }).
                Where(x => x.fit > 0).
                OrderByDescending(x => x.fit).
                ToList();

            // If there are any bindings
            if (matchingBindings.Any())
            {
                var bestMatch = matchingBindings.First();
                var bestMatches = matchingBindings.Where(x => x.binding.Host == bestMatch.binding.Host);
                if (bestMatch.fit == 100 || !bindingOptions.Flags.HasFlag(SSLFlags.CentralSSL))
                {
                    // All existing https bindings
                    var existing = bestMatches.
                        Where(x => x.binding.Protocol == "https").
                        Select(x => x.binding.BindingInformation).
                        ToList();

                    foreach (var match in bestMatches)
                    {
                        var isHttps = match.binding.Protocol == "https";
                        if (isHttps)
                        {
                            if (UpdateExistingBindingFlags(bindingOptions.Flags, match.binding, allBindings, out var updateFlags))
                            {
                                var updateOptions = bindingOptions.WithFlags(updateFlags);
                                UpdateBinding(site, match.binding, updateOptions);
                            }
                        } 
                        else
                        {
                            var addOptions = bindingOptions.WithHost(match.binding.Host);
                            // The existance of an HTTP binding with a specific IP overrules 
                            // the default IP.
                            if (addOptions.IP == IISClient.DefaultBindingIp &&
                                match.binding.IP != IISClient.DefaultBindingIp &&
                                !string.IsNullOrEmpty(match.binding.IP))
                            {
                                addOptions = addOptions.WithIP(match.binding.IP);
                            }

                            var binding = addOptions.Binding;
                            if (!existing.Contains(binding) && AllowAdd(addOptions, allBindings))
                            {
                                AddBinding(site, addOptions);
                                existing.Add(binding);
                            }
                        }
                    }
                    return bestMatch.binding.Host;
                }
            }

            // At this point we haven't even found a partial match for our hostname
            // so as the ultimate step we create new https binding
            if (AllowAdd(bindingOptions, allBindings))
            {
                AddBinding(site, bindingOptions);
                return bindingOptions.Host;
            }

            // We haven't been able to do anything
            return null;
        }

        /// <summary>
        /// Sanity checks, prevent bad bindings from messing up IIS
        /// </summary>
        /// <param name="start"></param>
        /// <param name="match"></param>
        /// <param name="allBindings"></param>
        /// <returns></returns>
        private bool AllowAdd(BindingOptions options, TBinding[] allBindings)
        {
            var bindingInfoShort = $"{options.IP}:{options.Port}";
            var bindingInfoFull = $"{bindingInfoShort}:{options.Host}";

            // On Windows 2008, which does not support SNI, only one 
            // https binding can exist for each IP/port combination
            if (_client.Version.Major < 8)
            {
                if (allBindings.Any(x => x.BindingInformation.StartsWith(bindingInfoShort)))
                {
                    _log.Warning($"Prevent adding duplicate binding for {bindingInfoShort}");
                    return false;
                }
            }

            // In general we shouldn't create duplicate bindings
            // because then only one of them will be usable at the
            // same time.
            if (allBindings.Any(x => x.BindingInformation == bindingInfoFull))
            {
                _log.Warning($"Prevent adding duplicate binding for {bindingInfoFull}");
                return false;
            }

            // Wildcard bindings are only supported in Windows 2016+
            if (options.Host.StartsWith("*.") && _client.Version.Major < 10)
            {
                _log.Warning($"Unable to create wildcard binding on this version of IIS");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Turn on SNI for #915
        /// </summary>
        /// <param name="start"></param>
        /// <param name="match"></param>
        /// <param name="allBindings"></param>
        /// <returns></returns>
        private bool UpdateExistingBindingFlags(SSLFlags start, TBinding match, TBinding[] allBindings, out SSLFlags modified)
        {
            modified = start;
            if (_client.Version.Major >= 8 && !match.SSLFlags.HasFlag(SSLFlags.SNI))
            {
                if (allBindings
                    .Except(new[] { match })
                    .Where(x => x.Port == match.Port)
                    .Where(x => StructuralComparisons.StructuralEqualityComparer.Equals(match.CertificateHash, x.CertificateHash))
                    .Where(x => !x.SSLFlags.HasFlag(SSLFlags.SNI))
                    .Any())
                {
                    if (!string.IsNullOrEmpty(match.Host))
                    {
                        _log.Warning("Turning on SNI for existing binding to avoid conflict");
                        modified = start | SSLFlags.SNI;
                    }
                    else
                    {
                        _log.Warning("Our best match was the default binding and it seems there are other non-SNI enabled " +
                            "bindings listening to the same endpoint, which means we cannot update it without potentially " +
                            "causing problems. Instead, a new binding will be created. You may manually update the bindings " +
                            "if you want IIS to be configured in a different way.");
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Make sure the flags are set correctly for updating the binding,
        /// because special conditions apply to the default binding
        /// </summary>
        /// <param name="host"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private SSLFlags CheckFlags(bool newBinding, string host, SSLFlags flags)
        {
            // SSL flags are not supported at all by Windows 2008
            if (_client.Version.Major < 8)
            {
                return SSLFlags.None;
            }
            // Do not allow CentralSSL flag to be set on the default binding
            if (string.IsNullOrEmpty(host))
            {
                if (flags.HasFlag(SSLFlags.CentralSSL))
                {
                    throw new InvalidOperationException("Central SSL is not supported without a hostname");
                }
            }
            // Add SNI on Windows Server 2012+
            if (newBinding)
            {
                if (!string.IsNullOrEmpty(host) && _client.Version.Major >= 8)
                {
                    flags |= SSLFlags.SNI;
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
        private void AddBinding(TSite site, BindingOptions options)
        {
            options = options.WithFlags(CheckFlags(true, options.Host, options.Flags));
            _log.Information(LogType.All, "Adding new https binding {binding}", options.Binding);
            _client.AddBinding(site, options);
        }

        private void UpdateBinding(TSite site, TBinding existingBinding, BindingOptions options)
        {
            // Check flags
            options = options.WithFlags(CheckFlags(false, existingBinding.Host, options.Flags));

            var currentFlags = existingBinding.SSLFlags;
            if ((currentFlags & ~SSLFlags.SNI) == (options.Flags & ~SSLFlags.SNI) && // Don't care about SNI status
                ((options.Store == null && existingBinding.CertificateStoreName == null) ||
                StructuralComparisons.StructuralEqualityComparer.Equals(existingBinding.CertificateHash, options.Thumbprint) &&
                string.Equals(existingBinding.CertificateStoreName, options.Store, StringComparison.InvariantCultureIgnoreCase)))
            {
                _log.Verbose("No binding update needed");
            }
            else
            {
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
                _log.Information(LogType.All, "Updating existing https binding {host}:{port}",
                    existingBinding.Host,
                    existingBinding.Port);
                _client.UpdateBinding(site, existingBinding, options);
            }
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
    }
}