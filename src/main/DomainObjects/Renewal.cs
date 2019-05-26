using Newtonsoft.Json;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PKISharp.WACS.DomainObjects
{
    /// <summary>
    /// Main unit of work for the program, contains all the information 
    /// required to generate a target, do the validation, store the resulting
    /// certificate somewhere and finally run installation steps to update 
    /// software.
    /// </summary>
    [DebuggerDisplay("Renewal {Id}: {FriendlyName}")]
    public class Renewal
    {
        internal static Renewal Create(string id, PasswordGenerator generator)
        {
            var ret = new Renewal
            {
                New = true,
                Id = string.IsNullOrEmpty(id) ? ShortGuid.NewGuid().ToString() : id,
                PfxPassword = generator.Generate()
            };
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
        /// Friendly name for the certificate. If left
        /// blank or empty, the CommonName will be used.
        /// </summary>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Display name, as the program shows this certificate
        /// in the interface. This is set to the most recently 
        /// used FriendlyName
        /// </summary>
        public string LastFriendlyName { get; set; }

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
        /// Next scheduled renew date (computed based on most recent succesful renewal)
        /// </summary>
        [JsonIgnore]
        public DateTime Date {
            get
            {
                var lastSuccess = History.LastOrDefault(x => x.Success)?.Date;
                if (lastSuccess.HasValue)
                {
                    return lastSuccess.
                        Value.
                        AddDays(Properties.Settings.Default.RenewalDays).
                        ToLocalTime();
                }
                else
                {
                    return new DateTime(1970, 1, 1);
                }
            }
        }

        /// <summary>
        /// Store information about TargetPlugin
        /// </summary>
        public TargetPluginOptions TargetPluginOptions { get; set; }

        /// <summary>
        /// Store information about ValidationPlugin
        /// </summary>
        public ValidationPluginOptions ValidationPluginOptions { get; set; }

        /// <summary>
        /// Store information about CsrPlugin
        /// </summary>
        public CsrPluginOptions CsrPluginOptions { get; set; }

        /// <summary>
        /// Store information about StorePlugin
        /// </summary>
        public StorePluginOptions StorePluginOptions { get; set; }

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
        public override string ToString() {
            var success = History.FindAll(x => x.Success).Count;
            var errors = History.AsEnumerable().Reverse().TakeWhile(x => !x.Success);
            if (errors.Count() > 0)
            {
                var error = History.Last().ErrorMessage;
                return $"{LastFriendlyName} - renewed {success} time{(success != 1 ? "s" : "")}, due after {Date.ToUserString()}, {errors.Count()} error(s) like '{error}'";
            }
            else
            {
                return $"{LastFriendlyName} - renewed {success} time{(success != 1 ? "s" : "")}, due after {Date.ToUserString()}";
            }

        }
    }
}
