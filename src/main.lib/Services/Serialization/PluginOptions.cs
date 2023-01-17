using PKISharp.WACS.Plugins;

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
        internal void Show(IInputService input, IPluginService plugin) {
            var meta = plugin.GetPlugin(this);
            input.Show(null, $"[{meta.Step}]");
            input.Show("Plugin", $"{meta.Name} - ({meta.Description})", level: 1);
        }

        /// <summary>
        /// Report additional settings to the user
        /// </summary>
        /// <param name="input"></param>
        public virtual void Show(IInputService input) { }
    }
}
