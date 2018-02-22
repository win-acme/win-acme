using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS
{
    public class ScheduledRenewal
    {
        /// <summary>
        /// Has this renewal been saved?
        /// </summary>
        [JsonIgnore]
        internal bool New { get; set; }

        /// <summary>
        /// Is this renewal a test?
        /// </summary>
        [JsonIgnore]
        internal bool Test { get; set; }

        /// <summary>
        /// Has this renewal been changed?
        /// </summary>
        [JsonIgnore]
        internal bool Updated { get; set; }

        /// <summary>
        /// Next scheduled renew date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Information about the certificate
        /// </summary>
        public Target Binding { get; set; }

        /// <summary>
        /// Location of the Central SSL store (if not specified, certificate
        /// is stored in the Certificate store instead). This takes priority
        /// over CertificateStore
        /// </summary>
        [JsonProperty(PropertyName = "CentralSsl")]
        public string CentralSslStore { get; set; }

        /// <summary>
        /// Name of the certificate store to use
        /// </summary>
        public string CertificateStore { get; set; }

        /// <summary>
        /// Shortcut
        /// </summary>
        [JsonIgnore]
        internal bool CentralSsl
        {
            get
            {
                return !string.IsNullOrWhiteSpace(CentralSslStore);
            }
        }

        /// <summary>
        /// Legacy, replaced by HostIsDns parameter on Target
        /// </summary>
        [Obsolete]
        public bool? San { get; set; }

        /// <summary>
        /// Do not delete previously issued certificate
        /// </summary>
        public bool? KeepExisting { get; set; }

        /// <summary>
        /// Name of the plugins to use for validation, in order of execution
        /// </summary>
        public List<string> InstallationPluginNames { get; set; }

        /// <summary>
        /// Script to run on succesful renewal
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Parameters for script
        /// </summary>
        public string ScriptParameters { get; set; }

        /// <summary>
        /// Warmup target website (applies to http-01 validation)
        /// TODO: remove
        /// </summary>
        public bool Warmup { get; set; }

        /// <summary>
        /// History for this renewal
        /// </summary>
        [JsonIgnore]
        public List<RenewResult> History { get; set; }

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Binding?.Host ?? "[unknown]"} - renew after {Date.ToUserString()}";

        /// <summary>
        /// Get the most recent thumbprint
        /// </summary>
        /// <returns></returns>
        [JsonIgnore]
        public string Thumbprint
        { 
            get
            {
                return History?.
                      OrderByDescending(x => x.Date).
                      Where(x => x.Success).
                      Select(x => x.Thumbprint).
                      FirstOrDefault();
            }
        }

        /// <summary>
        /// Find the most recently issued certificate for a specific target
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public CertificateInfo Certificate(IStorePlugin store)
        {
            var thumbprint = Thumbprint;
            var useThumbprint = !string.IsNullOrEmpty(thumbprint);
            if (useThumbprint)
            {
                return store.FindByThumbprint(thumbprint);
            }
            else
            {
                return null;
            }
        }

    }
}
