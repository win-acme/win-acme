using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    public class DnsRecord
    {
        [JsonPropertyName("record_id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int RecordId { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("ttl")]
        public int Ttl { get; set; }
    }
}
