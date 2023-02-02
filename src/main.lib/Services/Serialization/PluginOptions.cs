using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;

namespace PKISharp.WACS.Services.Serialization
{
    /// <summary>
    /// For initial JSON deserialization
    /// </summary>
    public class PluginOptionsBase
    {
        /// <summary>
        /// AcmeIdentifier for the plugin
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
        /// Provide plugin command line arguments to user
        /// </summary>
        /// <param name="input"></param>
        internal string Describe(ILifetimeScope sc, IAutofacBuilder ab, IPluginService plugin)
        {
            var meta = plugin.GetPlugin(this);
            var ts = ab.Target(sc, new Target(new DnsIdentifier("www.example.com")));
            var fs = ab.PluginFrontend<IPluginCapability, PluginOptionsBase>(ts, meta);
            var fe = fs.Resolve<PluginFrontend<IPluginCapability, PluginOptionsBase>>();
            return fe.OptionsFactory.Describe(this).Trim();
        }

        /// <summary>
        /// Report additional settings to the user
        /// </summary>
        /// <param name="input"></param>
        public virtual void Show(IInputService input) { }
    }
}
