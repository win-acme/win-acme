using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace PKISharp.WACS.DomainObjects
{
    [DebuggerDisplay("Renewal {Id}: {FriendlyName}")]
    public class Renewal
    {
        public static Renewal Create()
        {
            var ret = new Renewal
            {
                New = true,
                Id = ShortGuid.NewGuid().ToString()
            };

            // Set 256 bit random password that will be used to keep the .pfx file in the cache folder safe.
            RNGCryptoServiceProvider random = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[32];
            random.GetBytes(buffer);
            ret.PfxPassword = Convert.ToBase64String(buffer);

            return ret;
        }

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
        /// Has this renewal been deleted?
        /// </summary>
        [JsonIgnore]
        internal bool Deleted { get; set; }

        /// <summary>
        /// Is this renewal new
        /// </summary>
        [JsonIgnore]
        internal bool New { get; set; }

        /// <summary>
        /// Unique identifer for the renewal
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Friendly name for the certificate
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Encrypted (if enabled) version of the PfxFile password
        /// </summary>
        public string PfxPasswordProtected { get; set; }

        /// <summary>
        /// Plain text readable version of the PfxFile password
        /// </summary>
        [JsonIgnore]
        public string PfxPassword
        {
            get => PfxPasswordProtected.Unprotect();
            set => PfxPasswordProtected = value.Protect();
        }

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
        public override string ToString() => $"{FriendlyName} [{Id}] - due {Date.ToUserString()}";
    }
}
