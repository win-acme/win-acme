using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    public class ProductList
    {
        [JsonPropertyName("products")]
        public List<Product> Products { get; set; }
    }
}
