using PKISharp.WACS.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// Non-generic base class needed for serialization
    /// </summary>
    public abstract class PluginOptions
    {
        public PluginOptions() => Plugin = GetType().PluginId();

        /// <summary>
        /// Contains the unique GUID of the plugin
        /// </summary>
        public string? Plugin { get; set; }

        /// <summary>
        /// Describe the plugin to the user
        /// </summary>
        /// <param name="input"></param>
        public virtual void Show(IInputService input) { }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public abstract Type Instance { get; }

        /// <summary>
        /// Short name for the plugin to be shown in the menu and e-mails
        /// </summary>
        [JsonIgnore]
        public abstract string Name { get; }

        /// <summary>
        /// One-line description for the plugin to be shown in the menu
        /// </summary>
        [JsonIgnore]
        public abstract string Description { get; }
    }
}
