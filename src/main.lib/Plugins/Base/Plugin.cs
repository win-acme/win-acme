using Autofac;
using PKISharp.WACS.Plugins.Interfaces;
using System;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using static System.Formats.Asn1.AsnWriter;

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
        public IPluginMeta Meta { get; set; }
  
        public Plugin(Type source, IPluginMeta meta, Steps step)
        {
            Id = meta.Id;
            Runner = source;
            Meta = meta;
            Step = step;
        }
    }
}
