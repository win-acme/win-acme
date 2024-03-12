using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns.Models;

internal sealed class ZonesResponse
{
    [JsonPropertyName("zones")]
    public required Zone[] Zones { get; init; }

    [JsonPropertyName("meta")]
    public required Metadata Meta { get; init; }
}