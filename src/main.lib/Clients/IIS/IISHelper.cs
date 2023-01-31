using PKISharp.WACS.Extensions;
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
            public IISBindingOption(string hostUnicode, string hostPunycode)
            {
                HostUnicode = hostUnicode;
                HostPunycode = hostPunycode;
            }

            public long SiteId { get; set; }
            public IISSiteType SiteType { get; set; }
            public bool Secure { get; set; }
            public bool Wildcard => HostUnicode.StartsWith("*.");
            public string HostUnicode { get; private set; }
            public string HostPunycode { get; private set; }
            public int Port { get; set; }
            public string? Protocol { get; set; }

            public override string ToString()
            {
                if ((Protocol == "http" && Port != 80) ||
                    (Protocol == "https" && Port != 443))
                {
                    return $"{HostUnicode}:{Port} (Site {SiteId}, {Protocol})";
                }
                return $"{HostUnicode} (Site {SiteId})";
            }
        }

        internal class IISSiteOption
        {
            public IISSiteOption(string name, IEnumerable<string> hosts)
            {
                Name = name;
                Hosts = hosts.ToList();
            }

            public long Id { get; set; }
            public IISSiteType SiteType { get; set; }
            public string Name { get; }
            public bool Secure { get; set; }
            public List<string> Hosts { get; }
        }

        public const string WebTypeFilter = "http";
        public const string FtpTypeFilter = "ftp";
        private readonly IIISClient _iisClient;
        private readonly ILogService _log;
        private readonly IdnMapping _idnMapping;
        internal DomainParseService DomainParser { get; }

        public IISHelper(ILogService log, IIISClient iisClient, DomainParseService domainParser)
        {
            _log = log;
            _iisClient = iisClient;
            _idnMapping = new IdnMapping();
            DomainParser = domainParser;
        }

        internal List<IISBindingOption> GetBindings()
        {
            if (_iisClient.Version.Major == 0)
            {
                _log.Warning("IIS not found. Skipping scan.");
                return new List<IISBindingOption>();
            }
            return GetBindings(_iisClient.Sites).ToList();
        }

        private List<IISBindingOption> GetBindings(IEnumerable<IIISSite> sites)
        {
            // Get all bindings matched together with their respective sites
            _log.Debug("Scanning IIS bindings for hosts");
            var siteBindings = sites.
                SelectMany(site => site.Bindings, (site, binding) => new { site, binding }).
                Where(sb => !string.IsNullOrWhiteSpace(sb.binding.Host)).
                ToList();

            static string lookupKey(IIISSite site, IIISBinding binding) =>
                site.Id + "#" + binding.BindingInformation.ToLower();

            // Option: hide http bindings when there are already https equivalents
            var secure = siteBindings
                .Where(sb =>
                    sb.binding.Secure ||
                    sb.site.Bindings.Any(other =>
                        other.Secure &&
                        string.Equals(sb.binding.Host, other.Host, StringComparison.InvariantCultureIgnoreCase)))
                .ToDictionary(sb => lookupKey(sb.site, sb.binding));

            var targets = siteBindings.
                Select(sb => new
                {
                    host = sb.binding.Host.ToLower(),
                    sb.site,
                    sb.binding,
                    secure = secure.ContainsKey(lookupKey(sb.site, sb.binding))
                }).
                Select(sbi => new IISBindingOption(sbi.host, _idnMapping.GetAscii(sbi.host))
                {
                    SiteId = sbi.site.Id,
                    SiteType = sbi.site.Type,
                    Port = sbi.binding.Port,
                    Protocol = sbi.binding.Protocol,
                    Secure = sbi.secure
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
            var targets = GetSites(_iisClient.Sites).ToList();
            if (!targets.Any() && logInvalidSites)
            {
                _log.Warning("No applicable IIS sites were found.");
            }
            return targets;
        }

        private List<IISSiteOption> GetSites(IEnumerable<IIISSite> sites)
        {
            // Get all bindings matched together with their respective sites
            var secure = sites.Where(site =>
                site.Bindings.All(binding =>
                    binding.Secure ||
                    site.Bindings.Any(other =>
                        other.Secure &&
                        string.Equals(other.Host, binding.Host, StringComparison.InvariantCultureIgnoreCase)))).ToList();

            var targets = sites.
                Select(site => new IISSiteOption(site.Name, GetHosts(site))
                {
                    Id = site.Id,
                    SiteType = site.Type,
                    Secure = secure.Contains(site)
                }).
                ToList();
            return targets;
        }

        internal List<IISBindingOption> FilterBindings(List<IISBindingOption> bindings, IISOptions options)
        {
            // Check if we have any bindings
            _log.Verbose("{0} named bindings found in IIS", bindings.Count);

            // Filter by binding/site type
            _log.Debug("Filtering based on binding type");
                bindings = bindings.Where(x => {
                    return x.SiteType switch
                    {
                        IISSiteType.Web => options.IncludeTypes == null || options.IncludeTypes.Contains(WebTypeFilter),
                        IISSiteType.Ftp => options.IncludeTypes != null && options.IncludeTypes.Contains(FtpTypeFilter),
                        _ => false
                    };
                }). 
                ToList();

            // Filter by site
            if (options.IncludeSiteIds != null && options.IncludeSiteIds.Any())
            {
                _log.Debug("Filtering by site(s) {0}", options.IncludeSiteIds);
                bindings = bindings.Where(x => options.IncludeSiteIds.Contains(x.SiteId)).ToList();
                _log.Verbose("{0} bindings remaining after site filter", bindings.Count);
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
                _log.Verbose("{0} bindings remaining after host filter", bindings.Count);
            }
            else
            {
                _log.Verbose("No host filter applied");
            }

            // Remove exlusions
            if (options.ExcludeHosts != null && options.ExcludeHosts.Any())
            {
                bindings = bindings.Where(x => !options.ExcludeHosts.Contains(x.HostUnicode)).ToList();
                _log.Verbose("{0} named bindings remaining after explicit exclusions", bindings.Count);
            }

            // Check if we have anything left
            _log.Verbose($"{{0}} matching binding{(bindings.Count != 1 ? "s" : "")} found", bindings.Count);
            return bindings.ToList();
        }

        internal static bool Matches(IISBindingOption binding, Regex regex)
        {
            return regex.IsMatch(binding.HostUnicode)
                || regex.IsMatch(binding.HostPunycode);
        }

        internal string HostsToRegex(IEnumerable<string> hosts) =>
            $"^({string.Join('|', hosts.Select(x => Regex.Escape(x)))})$";

        private Regex? GetRegex(IISOptions options)
        {
            if (!string.IsNullOrEmpty(options.IncludePattern))
            {
                return new Regex(options.IncludePattern.PatternToRegex());
            }
            if (options.IncludeHosts != null && options.IncludeHosts.Any())
            {
                return new Regex(HostsToRegex(options.IncludeHosts));
            }
            if (!string.IsNullOrEmpty(options.IncludeRegex))
            {
                return new Regex(options.IncludeRegex);
            }
            return null;
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
