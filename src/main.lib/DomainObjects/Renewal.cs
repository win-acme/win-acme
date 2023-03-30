using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Serialization;

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
        internal static Renewal Create(string? id = null)
        {
            var ret = new Renewal
            {
                New = true,
                Id = string.IsNullOrEmpty(id) ? ShortGuid.NewGuid().ToString() : id,
                PfxPassword = new ProtectedString(PasswordGenerator.Generate())
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
        public string Id { get; set; } = "";

        /// <summary>
        /// Friendly name for the certificate. If left
        /// blank or empty, the CommonName will be used.
        /// </summary>
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Display name, as the program shows this certificate
        /// in the interface. This is set to the most recently 
        /// used FriendlyName
        /// </summary>
        public string? LastFriendlyName { get; set; }

        /// <summary>
        /// Plain text readable version of the PfxFile password
        /// </summary>
        [JsonPropertyName("PfxPasswordProtected")]
        public ProtectedString? PfxPassword { get; set; }

        /// <summary>
        /// Store information about TargetPlugin
        /// </summary>
        public TargetPluginOptions TargetPluginOptions { get; set; } = new TargetPluginOptions();

        /// <summary>
        /// Store information about ValidationPlugin
        /// </summary>
        public ValidationPluginOptions ValidationPluginOptions { get; set; } = new ValidationPluginOptions();

        /// <summary>
        /// Store information about CsrPlugin
        /// </summary>
        public CsrPluginOptions? CsrPluginOptions { get; set; }

        /// <summary>
        /// Store information about OrderPlugin
        /// </summary>
        public OrderPluginOptions? OrderPluginOptions { get; set; }

        /// <summary>
        /// Store information about StorePlugin
        /// </summary>
        public List<StorePluginOptions> StorePluginOptions { get; set; } = new List<StorePluginOptions>();

        /// <summary>
        /// Store information about InstallationPlugins
        /// </summary>
        public List<InstallationPluginOptions> InstallationPluginOptions { get; set; } = new List<InstallationPluginOptions>();

        /// <summary>
        /// History for this renewal
        /// </summary>
        public List<RenewResult> History { get; set; } = new List<RenewResult>();

        /// <summary>
        /// Which ACME account is associated with the renewal (null = default)
        /// </summary>
        public string? Account { get; set; }

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString(null, null);

        /// <summary>
        /// Pretty format
        /// </summary>
        /// <returns></returns>
        public string ToString(DueDateStaticService? dueDateService, IInputService? inputService)
        {
            var success = History.FindAll(x => x.Success == true).Count;
            var errors = History.AsEnumerable().Reverse().TakeWhile(x => x.Success == false);
            var ret = $"{LastFriendlyName} - renewed {success} time{(success != 1 ? "s" : "")}";

            var format = (DateTime date) =>
            {
                if (inputService != null)
                {
                    return inputService.FormatDate(date);
                }
                else
                {
                    return date.ToShortDateString();
                }
            };

            if (dueDateService != null)
            {
                var due = dueDateService.IsDue(this);
                if (!due)
                {
                    var dueDate = dueDateService.DueDate(this);
                    if (dueDate != null)
                    {
                        if (dueDate.Start != dueDate.End) 
                        {
                            ret += $", due between {format(dueDate.Start)} and {format(dueDate.End)}";
                        }
                        else
                        {
                            ret += $", due after {format(dueDate.Start)}";
                        }
                    }
                } 
                else
                {
                    ret += ", due now";
                }
            }
            if (errors.Any())
            {
                ret += $", {errors.Count()} error{(errors.Count() != 1 ? "s" : "")}";
            }
            return ret;
        }
    }
}
