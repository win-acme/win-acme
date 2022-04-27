using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    public class DnsRecordList
    {
        [JsonPropertyName("records")]
        public List<DnsRecord> Records { get; set; }
    }
}
