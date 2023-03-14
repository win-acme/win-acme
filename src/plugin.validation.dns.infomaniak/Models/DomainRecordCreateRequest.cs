using Newtonsoft.Json;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Models;

internal class DomainRecordCreateRequest
{
    [JsonProperty("type")]
    public string Type { get; init; } = string.Empty;
    [JsonProperty("source")]
    public string Source { get; init; } = string.Empty;
    [JsonProperty("target")]
    public string Target { get; init; } = string.Empty;
    [JsonProperty("ttl")]
    public int TimeToLive { get; init; }
}