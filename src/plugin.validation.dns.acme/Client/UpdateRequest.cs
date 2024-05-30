using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class UpdateRequest
    {
        [JsonPropertyName("subdomain")]
        public string Subdomain { get; set; } = "";
        [JsonPropertyName("txt")]
        public string Token { get; set; } = "";
    }
}
