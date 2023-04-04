using Newtonsoft.Json;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Models;

internal class DomainRecordCreateResponse
{
    [JsonProperty("data")]
    public int Data { get; set; }
}