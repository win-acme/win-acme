using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    /// <summary>
    /// List IIS sites that can be used as valid targets.
    /// Used by IISSite and IISSites plugin, as well as
    /// their respective OptionsFactories
    /// </summary>
    internal class IISSiteHelper
    {
        internal class IISSiteOption
        {
            public long Id { get; set; }
            public string Name { get; set; }
            public bool Https { get; set; }
            public List<string> Hosts { get; set; }
        }

        private readonly IIISClient _iisClient;
        private readonly ILogService _log;

        public IISSiteHelper(ILogService log, IIISClient iisClient)
        {
            _log = log;
            _iisClient = iisClient;
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
                OrderBy(target => target.Name).
                ToList();

            if (!targets.Any() && logInvalidSites)
            {
                _log.Warning("No applicable IIS sites were found.");
            }
            return targets;
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