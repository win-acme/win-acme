using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns.Models;


internal sealed class ZoneResponse
{
    [JsonPropertyName("zone")]
    public required ZoneDetails Zone { get; init; }
}

internal sealed class ZoneDetails
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("paused")]
    public bool Paused { get; init; }

    [JsonPropertyName("status")]
    public required ZoneStatus Status { get; init; }

    [JsonPropertyName("ttl")]
    public required uint Ttl { get; init; }
}