using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LetsEncrypt.ACME.Simple
{
    public class Target
    {
        public static readonly IReadOnlyDictionary<string, Plugin> Plugins;

        static Target()
        {
            Plugins = Assembly.GetExecutingAssembly()
                              .GetTypes()
                              .Where(type => type.BaseType == typeof(Plugin))
                              .Select(type => type.GetConstructor(Type.EmptyTypes).Invoke(null))
                              .Cast<Plugin>()
                              .ToDictionary(plugin => plugin.Name);
        }

        public string Host { get; set; }
        public string WebRootPath { get; set; }
        public long SiteId { get; set; }
        public List<string> AlternativeNames { get; set; } = new List<string>();
        public string PluginName { get; set; } = "IIS";
        public Plugin Plugin => Plugins[PluginName];

        public override string ToString() {
            var x = new StringBuilder();
            x.Append($"[{PluginName}] ");
            if (!AlternativeNames.Contains(Host))
            {
                x.Append($"{Host} ");
            }
            if (SiteId > 0)
            {
                x.Append($"(SiteId {SiteId}) ");
            }
            var num = AlternativeNames.Count();
            x.Append($"[{num} binding");
            if (num != 1)
            {
                x.Append($"s");
            }
            if (num > 0)
            {
                x.Append($" - {AlternativeNames.First()}");
            }
            if (num > 1)
            {
                x.Append($", ...");
            }
            if (!string.IsNullOrWhiteSpace(WebRootPath))
            {
                x.Append($" @ {WebRootPath}");
            }
            x.Append("]");
            return x.ToString();
        }
    }
}