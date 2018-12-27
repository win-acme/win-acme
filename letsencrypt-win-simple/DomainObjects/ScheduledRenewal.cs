using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.Base.Options;

namespace PKISharp.WACS.DomainObjects
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
        public Target Target { get; set; }

        /// <summary>
        /// Name of the plugins to use for validation, in order of execution
        /// </summary>
        public List<string> InstallationPluginNames { get; set; }

        /// <summary>
        /// Store information about StorePlugin
        /// </summary>
        public StorePluginOptions StorePluginOptions { get; set; }

        /// <summary>
        /// Store information about ValidationPlugin
        /// </summary>
        public ValidationPluginOptions ValidationPluginOptions { get; set; }

        /// <summary>
        /// Script to run on succesful renewal
        /// </summary>
        public string Script { get; set; }

        /// <summary>
        /// Parameters for script
        /// </summary>
        public string ScriptParameters { get; set; }

        /// <summary>
        /// History for this renewal
        /// </summary>
        public List<RenewResult> History { get; set; }

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Target?.Host ?? "[unknown]"} - renew after {Date.ToUserString()}";

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
