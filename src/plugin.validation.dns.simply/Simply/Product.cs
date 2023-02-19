using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    public class Product
    {
        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("domain")]
        public ProductDomain? Domain { get; set; }
    }
}
