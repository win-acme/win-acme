using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    public class ProductDomain
    {
        /// <summary>
        /// E.g. øbo.dk
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// E.g. xn--bo-kka.dk
        /// </summary>
        [JsonPropertyName("name_idn")]
        public string? NameIdn { get; set; }
    }
}
