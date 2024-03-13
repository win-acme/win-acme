using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns.Models;

internal sealed class Metadata
{
    [JsonPropertyName("pagination")]
    public required PaginationMetadata Pagination { get; init; }
}