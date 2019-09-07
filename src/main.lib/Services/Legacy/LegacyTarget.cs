using System.Collections.Generic;

namespace PKISharp.WACS.Services.Legacy
{
    internal class LegacyTarget
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
        /// IP address to create new SSL bindings on
        /// </summary>
        public string SSLIPAddress { get; set; }

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
        public LegacyCredentials HttpFtpOptions { get; set; }

        /// <summary>
        /// Options for ValidationPlugins.Http.WebDav
        /// </summary>
        public LegacyCredentials HttpWebDavOptions { get; set; }

        /// <summary>
        /// Options for ValidationPlugins.Dns.Azure
        /// </summary>
        public LegacyAzureDnsOptions DnsAzureOptions { get; set; }

        /// <summary>
        /// Options for ValidationPlugins.Dns.Script
        /// </summary>
        public LegacyDnsScriptOptions DnsScriptOptions { get; set; }

        /// <summary>
        /// Legacy
        /// </summary>
        public string PluginName { get; set; }
    }
}