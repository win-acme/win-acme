using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// For initial JSON deserialization
    /// </summary>
    public class PluginOptionsBase
    {
        public string? Plugin { get; set; }

        /// <summary>
        /// Find plugin based on available metadata or type
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Plugin? FindPlugin(IPluginService plugin)
        {
            if (string.IsNullOrWhiteSpace(Plugin))
            {
                // Find plugin based on the type
                var typeMatch = plugin.GetPlugins().FirstOrDefault(x => x.Meta.Options == GetType());
                return typeMatch;
            }
            else if (Guid.TryParse(Plugin, out var pluginGuid))
            {
                return plugin.GetPlugin(pluginGuid);
            }
            return null;
        }

        /// <summary>
        /// Name of the plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public string Name(IPluginService plugin) => FindPlugin(plugin)?.Meta.Name ?? "?";
        public string Description(IPluginService plugin) => FindPlugin(plugin)?.Meta.Description ?? "?";
    }

    /// <summary>
    /// Non-generic base class needed for serialization
    /// </summary>
    public abstract class PluginOptions : PluginOptionsBase
    {
        /// <summary>
        /// Describe the plugin to the user
        /// </summary>
        /// <param name="input"></param>
        public virtual void Show(IInputService input) { }

        [JsonIgnore]
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
