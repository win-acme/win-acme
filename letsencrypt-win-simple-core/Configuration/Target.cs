using System.Collections.Generic;

namespace LetsEncrypt.ACME.Simple.Core.Configuration
{
    public class Target
    {
        public string Host { get; set; }
        public string PfxPassword { get; set; }
        public string WebRootPath { get; set; }
        public long SiteId { get; set; }
        public List<string> AlternativeNames { get; set; }
        public string PluginName { get; set; } = "IIS";

        public override string ToString() => $"{PluginName} {Host} ({WebRootPath})";
    }
}