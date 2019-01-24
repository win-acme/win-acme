using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace PKISharp.WACS.Clients.IIS
{
    internal class IISBindingHelper
    {
        internal class IISBindingOption
        {
            public long SiteId { get; set; }
            public bool Hidden { get; set; }
            public string HostUnicode { get; set; }
            public string HostPunycode { get; set; }
        }

        private IIISClient _iisClient;
        private ILogService _log;
        private readonly IdnMapping _idnMapping;

        public IISBindingHelper(ILogService log, IIISClient iisClient)
        {
            _log = log;
            _iisClient = iisClient;
            _idnMapping = new IdnMapping();
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
                Where(sb => !string.IsNullOrWhiteSpace(sb.binding.Host));

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
                    host = sb.binding.Host.ToLower(),
                    sb.site,
                    sb.binding,
                    hidden = hidden.Contains(sb)
                }).
                Select(sbi => new IISBindingOption
                {
                    SiteId = sbi.site.Id,
                    HostUnicode = sbi.host,
                    HostPunycode = _idnMapping.GetAscii(sbi.host),
                    Hidden = sbi.hidden
                }).
                DistinctBy(t => t.HostUnicode).
                OrderBy(t => t.HostUnicode).
                ToList();

            if (!targets.Any() && logInvalidSites)
            {
                _log.Warning("No IIS bindings with host names were found. A host name is required to verify domain ownership.");
            }
            return targets;
        }
    }
}
