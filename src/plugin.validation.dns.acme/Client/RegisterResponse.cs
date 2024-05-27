using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    public class RegisterResponse
    {
        [JsonPropertyName("username")]
        public string UserName { get; set; } = "";
        [JsonPropertyName("password")]
        public string Password { get; set; } = "";
        [JsonPropertyName("fulldomain")]
        public string Fulldomain { get; set; } = "";
        [JsonPropertyName("subdomain")]
        public string Subdomain { get; set; } = "";
    }
}
