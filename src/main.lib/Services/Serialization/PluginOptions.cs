using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// For initial JSON deserialization
    /// </summary>
    public class PluginOptionsBase
    {
        /// <summary>
        /// Identifier for the plugin
        /// </summary>
        public string? Plugin { get; set; }
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
        public void Show(IInputService input, IPluginService plugin) {
            var meta = plugin.GetPlugin(this);
            input.Show(null, $"[{meta.Step}]");
            input.Show("Plugin", $"{meta.Name} - ({meta.Description})", level: 1);
        }      

        /// <summary>
        /// Report additional settings to the user
        /// </summary>
        /// <param name="input"></param>
        public virtual void Show(IInputService input) { }

        [JsonIgnore]
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public abstract Type Instance { get; }
    }
}
