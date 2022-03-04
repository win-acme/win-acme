using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    internal class DnsRecordList
    {
        [JsonPropertyName("records")]
        public List<DnsRecord> Records { get; set; }
    }
}
