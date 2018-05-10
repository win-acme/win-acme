using Autofac;
using PKISharp.WACS.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PKISharp.WACS
{
    public class Target
    {
        /// <summary>
        /// Friendly name of the certificate, which may or may
        /// no also be the common name (first host), as indicated
        /// by the <see cref="HostIsDns"/> property.
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// Is the name of the certificate also a DNS identifier?
        /// </summary>
        public bool? HostIsDns { get; set; }

        /// <summary>
        /// The common name of the certificate. Has to be one of
        /// the alternative names.
        /// </summary>
        public string CommonName { get; set; }

        /// <summary>
        /// Hide target from the user in interactive mode, i.e.
        /// because some filter has been applied (--hidehttps).
        /// </summary>
        [JsonIgnore] public bool? Hidden { get; set; }

        /// <summary>
        /// Triggers IIS specific behaviours, such as copying
        /// the web.config file in case of Http validation
        /// </summary>
        public bool? IIS { get; set; }

        /// <summary>
        /// Path to use for Http validation (may be local or remote)
        /// </summary>
        public string WebRootPath { get; set; }

        /// <summary>
        /// Identify the IIS website that the target is based on
        /// </summary>
        [Obsolete]
        public long? SiteId { get; set; }

        /// <summary>
        /// Site used to get bindings from
        /// </summary>
        public long? TargetSiteId { get; set; }

        /// <summary>
        /// Site used to handle validation requests
        /// </summary>
        public long? ValidationSiteId { get; set; }

        /// <summary>
        /// Site used to install newly detected bindings
        /// </summary>
        public long? InstallationSiteId { get; set; }

        /// <summary>
        /// FTP Site used to install newly detected bindings
        /// </summary>
        public long? FtpSiteId { get; set; }

        /// <summary>
        /// Port to create new SSL bindings on
        /// </summary>
        public int? SSLPort { get; set; }

        /// <summary>
        /// Port to use to listen to HTTP-01 validation requests
        /// </summary>
        public int? ValidationPort { get; set; }

        /// <summary>
        /// List of bindings to exclude from the certificate
        /// </summary>
        public string ExcludeBindings { get; set; }

        /// <summary>
        /// List of alternative names for the certificate
        /// </summary>
        public List<string> AlternativeNames { get; set; } = new List<string>();

        /// <summary>
        /// Name of the plugin to use for refreshing the target
        /// </summary>
        public string TargetPluginName { get; set; }

        /// <summary>
        /// Name of the plugin to use for validation
        /// </summary>
        public string ValidationPluginName { get; set; }

        /// <summary>
        /// Options for ValidationPlugins.Http.Ftp
        /// </summary>
        public FtpOptions HttpFtpOptions { get; set; }

        /// <summary>
        /// Options for ValidationPlugins.Http.WebDav
        /// </summary>
        public WebDavOptions HttpWebDavOptions { get; set; }

        /// <summary>
        /// Options for ValidationPlugins.Dns.Azure
        /// </summary>
        public AzureDnsOptions DnsAzureOptions { get; set; }

        /// <summary>
        /// Options for ValidationPlugins.Dns.Script
        /// </summary>
        public DnsScriptOptions DnsScriptOptions { get; set; }

        /// <summary>
        /// Legacy
        /// </summary>
        [Obsolete]
        public string PluginName { get; set; }

        /// <summary>
        /// Pretty print information about the target
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            var x = new StringBuilder();
            x.Append($"[{TargetPluginName}] ");
            if (!AlternativeNames.Contains(Host))
            {
                x.Append($"{Host} ");
            }
            if ((TargetSiteId ?? 0) > 0)
            {
                x.Append($"(SiteId {TargetSiteId.Value}) ");
            }
            x.Append("[");
            var num = AlternativeNames.Count();
            if (num > 0)
            {
                x.Append($"{num} binding");
                if (num > 1)
                {
                    x.Append($"s");
                }
                x.Append($" - {AlternativeNames.First()}");
                if (num > 1)
                {
                    x.Append($", ...");
                }
            }
            if (!string.IsNullOrWhiteSpace(WebRootPath))
            {
                x.Append($" @ {WebRootPath.Trim()}");
            }
            x.Append("]");
            return x.ToString();
        }

        /// <summary>
        /// Parse list of excluded hosts
        /// </summary>
        /// <returns></returns>
        public List<string> GetExcludedHosts()
        {
            var exclude = new List<string>();
            if (!string.IsNullOrEmpty(ExcludeBindings))
            {
                exclude = ExcludeBindings.Split(',').Select(x => x.ToLower().Trim()).ToList();
            }
            return exclude;
        }

        /// <summary>
        /// Parse unique DNS identifiers that the certificate should be 
        /// created for, taking into account the list of exclusions,
        /// support for IDNs and the limits of the ACME server
        /// </summary>
        /// <param name="unicode"></param>
        /// <returns></returns>
        public List<string> GetHosts(bool unicode, bool allowZero = false)
        {
            var hosts = new List<string>();
            if (HostIsDns == true)
            {
                hosts.Add(Host);
            }
            if (AlternativeNames != null && AlternativeNames.Any())
            {
                hosts.AddRange(AlternativeNames);
            }
            var exclude = GetExcludedHosts();
            var filtered = hosts.
                Where(x => !string.IsNullOrWhiteSpace(x)).
                Distinct().
                Except(exclude);

            if (unicode)
            {
                var idn = new IdnMapping();
                filtered = filtered.Select(x => idn.GetUnicode(x));
            }

            if (!filtered.Any())
            {
                if (!allowZero)
                {
                    throw new Exception("No DNS identifiers found.");
                }
            }
            else if (filtered.Count() > Constants.maxNames)
            {
                throw new Exception($"Too many hosts for a single certificate. ACME has a maximum of {Constants.maxNames}.");
            }
            return filtered.ToList();
        }
    }
}