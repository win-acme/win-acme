using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Diagnostics;

namespace PKISharp.WACS.Plugins
{
    /// <summary>
    /// Metadata for a specific plugin
    /// </summary>
    [DebuggerDisplay("{Backend.Name}")]
    public class Plugin
    {
        public Guid Id { get; set; }
        public Steps Step { get; set; }
        public Type Backend { get; set; }
        private IPluginMeta Meta { get; set; }
        public string Name => Meta.Name;
        public string Description => Meta.Description;
        public bool Hidden => Meta.Hidden;
        public Type Options => Meta.Options;
        public Type OptionsFactory => Meta.OptionsFactory;
        public Type OptionsJson => Meta.OptionsJson;
        public Type Capability => Meta.Capability;

        public Plugin(Type source, IPluginMeta meta, Steps step)
        {
            Id = meta.Id;
            Backend = source;
            Meta = meta;
            Step = step;
        }
    }
}
