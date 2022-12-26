using Autofac;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace PKISharp.WACS.Plugins.Base
{
    /// <summary>
    /// Metadata for a specific plugin
    /// </summary>
    [DebuggerDisplay("{Runner.Name}")]
    public class Plugin
    {
        public Guid Id { get; set; }
        public Steps Step { get; set; }
        public Type Runner { get; set; }
        public Type Options { get; set; }
        public Type Factory { get; set; }

        public Plugin(Type source, Plugin2Attribute meta, Steps step)
        {
            Id = meta.Id;
            Runner = source;
            Options = meta.Options;
            Factory = meta.OptionsFactory;
            Step = step;
        }
    }
}
