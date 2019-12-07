using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Clients.IIS
{
    internal class IISHelper
    {
        internal class IISBindingOption
        {
            public long SiteId { get; set; }
            public bool Https { get; set; }
            public string HostUnicode { get; set; }
            public string HostPunycode { get; set; }
            public int Port { get; set; }
            public string Protocol { get; set; }

            public override string ToString()
            {
                if ((Protocol == "http" && Port != 80) ||
                    (Protocol == "https" && Port != 443))
                {
                    return $"{HostUnicode}:{Port} (SiteId {SiteId}, {Protocol})";
                }
                return $"{HostUnicode} (SiteId {SiteId})";

            }
        }

        internal class IISSiteOption
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public bool Https { get; set; }
            public List<string> Hosts { get; set; }
        }

        private readonly IIISClient _iisClient;
        private readonly ILogService _log;
        private readonly IdnMapping _idnMapping;

        public IISHelper(ILogService log, IIISClient iisClient)
        {
            _log = log;
            _iisClient = iisClient;
            _idnMapping = new IdnMapping();
        }

        internal List<IISBindingOption> GetBindings()
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
                ToList();

            // Option: hide http bindings when there are already https equivalents
            var https = siteBindings.Where(sb => 
                sb.binding.Protocol == "https" ||
                sb.site.Bindings.Any(other => 
                    other.Protocol == "https" &&
                    string.Equals(sb.binding.Host, other.Host, StringComparison.InvariantCultureIgnoreCase))).ToList();

            var targets = siteBindings.
                Select(sb => new
                {
                    host = sb.binding.Host.ToLower(),
                    sb.site,
                    sb.binding,
                    https = https.Contains(sb)
                }).
                Select(sbi => new IISBindingOption
                {
                    SiteId = sbi.site.Id,
                    HostUnicode = sbi.host,
                    HostPunycode = _idnMapping.GetAscii(sbi.host),
                    Port = sbi.binding.Port,
                    Protocol = sbi.binding.Protocol,
                    Https = sbi.https
                }).
                DistinctBy(t => t.HostUnicode + "@" + t.SiteId).
                ToList();

            return targets;
        }

        internal List<IISSiteOption> GetSites(bool logInvalidSites)
        {
            if (_iisClient.Version.Major == 0)
            {
                _log.Warning("IIS not found. Skipping scan.");
                return new List<IISSiteOption>();
            }

            // Get all bindings matched together with their respective sites
            _log.Debug("Scanning IIS sites");
            var sites = _iisClient.WebSites.ToList();
            var https = sites.Where(site =>
                site.Bindings.All(binding =>
                    binding.Protocol == "https" ||
                    site.Bindings.Any(other =>
                        other.Protocol == "https" &&
                        string.Equals(other.Host, binding.Host, StringComparison.InvariantCultureIgnoreCase)))).ToList();

            var targets = sites.
                Select(site => new IISSiteOption
                {
                    Id = site.Id,
                    Name = site.Name,
                    Https = https.Contains(site),
                    Hosts = GetHosts(site)
                }).
                ToList();

            if (!targets.Any() && logInvalidSites)
            {
                _log.Warning("No applicable IIS sites were found.");
            }
            return targets;
        }

        internal List<IISBindingOption> FilterBindings(IISBindingsOptions options)
        {
            // Check if we have any bindings
            var bindings = GetBindings();
            _log.Verbose("{0} named bindings found in IIS", bindings.Count());
            if (options.IncludeSiteIds != null && options.IncludeSiteIds.Any())
            {
                _log.Debug("Filtering by site(s) {0}", options.IncludeSiteIds);
                bindings = bindings.Where(x => options.IncludeSiteIds.Contains(x.SiteId)).ToList();
                _log.Verbose("{0} bindings remaining after site filter", bindings.Count());
            }
            else
            {
                _log.Verbose("No site filter applied");
            }

            // Filter by pattern
            var regex = GetRegex(options);
            if (regex != null)
            {
                _log.Debug("Filtering by host: {regex}", regex);
                bindings = bindings.Where(x => Matches(x, regex)).ToList();
                _log.Verbose("{0} bindings remaining after host filter", bindings.Count());
            }
            else
            {
                _log.Verbose("No host filter applied");
            }

            // Remove exlusions
            if (options.ExcludeHosts != null && options.ExcludeHosts.Any())
            {
                bindings = bindings.Where(x => !options.ExcludeHosts.Contains(x.HostUnicode)).ToList();
                _log.Verbose("{0} named bindings remaining after explicit exclusions", bindings.Count());
            }

            // Check if we have anything left
            _log.Verbose("{0} matching bindings found", bindings.Count());
            return bindings;
        }

        internal bool Matches(IISBindingOption binding, Regex regex)
        {
            return regex.IsMatch(binding.HostUnicode)
                || regex.IsMatch(binding.HostPunycode);
        }

        internal string PatternToRegex(string pattern) =>
            $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";

        internal string HostsToRegex(IEnumerable<string> hosts) =>
            $"^({string.Join('|', hosts.Select(x => Regex.Escape(x)))})$";

        private Regex? GetRegex(IISBindingsOptions options)
        {
            if (!string.IsNullOrEmpty(options.IncludePattern))
            {
                return new Regex(PatternToRegex(options.IncludePattern));
            }
            if (options.IncludeHosts != null && options.IncludeHosts.Any())
            {
                return new Regex(HostsToRegex(options.IncludeHosts));
            }
            return options.IncludeRegex;
        }

        private List<string> GetHosts(IIISSite site)
        {
            return site.Bindings.Select(x => x.Host.ToLower()).
                            Where(x => !string.IsNullOrWhiteSpace(x)).
                            Distinct().
                            ToList();
        }
    }
}
