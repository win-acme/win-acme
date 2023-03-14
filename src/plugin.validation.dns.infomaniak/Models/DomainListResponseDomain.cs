using Newtonsoft.Json;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Models;

internal class DomainListResponseDomain
{
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("customer_name")]
    public string? CustomerName { get; set; }
}