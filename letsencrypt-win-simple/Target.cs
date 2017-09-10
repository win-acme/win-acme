using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LetsEncrypt.ACME.Simple
{
    public class Target
    {
        public string Host { get; set; }
        public bool? HostIsDns { get; set; }
        public string WebRootPath { get; set; }
        public long SiteId { get; set; }
        public List<string> AlternativeNames { get; set; } = new List<string>();
        public string PluginName { get; set; } = IISPlugin.PluginName;
        public Plugin Plugin => Program.Plugins.GetByName(Program.Plugins.Legacy, PluginName);

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
            x.Append("[");
            var num = AlternativeNames.Count();
            if (num > 0)
            {
                x.Append($"{num} binding");
                if (num > 1)
                {
                    x.Append($"s");
                }
                x.Append($" - {AlternativeNames.First()}");
                if (num > 1)
                {
                    x.Append($", ...");
                }
                x.Append($" ");
            }
            if (!string.IsNullOrWhiteSpace(WebRootPath))
            {
                x.Append($"@ {WebRootPath}");
            }
            x.Append("]");
            return x.ToString();
        }
    }
}