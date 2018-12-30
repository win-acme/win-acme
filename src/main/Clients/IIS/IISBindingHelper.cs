using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    internal class IISBindingHelper
    {
        internal class IISBindingOption
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public bool Hidden { get; set; }
            public string Host { get; set; }
        }

        private IIISClient _iisClient;
        private ILogService _log;

        public IISBindingHelper(ILogService log, IIISClient iisClient)
        {
            _log = log;
            _iisClient = iisClient;
        }

        internal List<IISBindingOption> GetBindings(bool hideHttps, bool logInvalidSites)
        {
            if (_iisClient.Version.Major == 0)
            {
                _log.Warning("IIS not found. Skipping scan.");
                return new List<IISBindingOption>();
            }

            // Get all bindings matched together with their respective sites
            _log.Debug("Scanning IIS site bindings for hosts");
            var siteBindings = _iisClient.WebSites.
                SelectMany(site => site.Bindings, (site, binding) => new { site, binding }).
                Where(sb => !string.IsNullOrWhiteSpace(sb.binding.Host)).
                Where(sb => !sb.binding.Host.StartsWith("*"));

            // Option: hide http bindings when there are already https equivalents
            var hidden = siteBindings.Take(0);
            if (hideHttps)
            {
                hidden = siteBindings.
                    Where(sb => sb.binding.Protocol == "https" ||
                                sb.site.Bindings.Any(other => other.Protocol == "https" &&
                                                                string.Equals(sb.binding.Host, other.Host, StringComparison.InvariantCultureIgnoreCase)));
            }

            var targets = siteBindings.
                Select(sb => new {
                    idn = _iisClient.IdnMapping.GetAscii(sb.binding.Host.ToLower()),
                    sb.site,
                    sb.binding,
                    hidden = hidden.Contains(sb)
                }).
                Select(sbi => new IISBindingOption
                {
                    Id = sbi.site.Id,
                    Host = sbi.idn,
                    Hidden = sbi.hidden
                }).
                DistinctBy(t => t.Host).
                OrderBy(t => t.Host).
                ToList();

            if (!targets.Any() && logInvalidSites)
            {
                _log.Warning("No IIS bindings with host names were found. A host name is required to verify domain ownership.");
            }
            return targets;
        }
    }
}
