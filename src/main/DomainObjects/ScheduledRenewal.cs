using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using System;
using System.Collections.Generic;

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
        /// Friendly name for the certificate
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Next scheduled renew date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Store information about TargetPlugin
        /// </summary>
        public TargetPluginOptions TargetPluginOptions { get; set; }

        /// <summary>
        /// Store information about StorePlugin
        /// </summary>
        public StorePluginOptions StorePluginOptions { get; set; }

        /// <summary>
        /// Store information about ValidationPlugin
        /// </summary>
        public ValidationPluginOptions ValidationPluginOptions { get; set; }

        /// <summary>
        /// Store information about InstallationPlugins
        /// </summary>
        public List<InstallationPluginOptions> InstallationPluginOptions { get; set; } = new List<InstallationPluginOptions>();

        /// <summary>
        /// History for this renewal
        /// </summary>
        public List<RenewResult> History { get; set; } = new List<RenewResult>();

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{FriendlyName} - renew after {Date.ToUserString()}";
    }
}
