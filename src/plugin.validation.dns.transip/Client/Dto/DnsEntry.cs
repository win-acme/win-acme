using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;

namespace TransIp.Library.Dto
{
    [DebuggerDisplay("{Name} [{Type}] {Content} ({Expire})")]
    public class DnsEntry
    {
        [JsonProperty("name")]
        public string? Name { get; set; }

        [JsonProperty("expire")]
        public int Expire { get; set; } = 3600;
            
        [JsonProperty("type")]
        public string? Type { get; set; }
            
        [JsonProperty("content")]
        public string? Content { get; set; }
    }

    public class DnsEntryList
    {
        [JsonProperty("dnsEntries")]
        public IEnumerable<DnsEntry>? DnsEntries { get; set; }
    }       
        
    [DebuggerDisplay("{DnsEntry}")]
    public class DnsEntryWrapper
    {        
        [JsonProperty("dnsEntry")]
        public DnsEntry? DnsEntry { get; set; }
    }
}
