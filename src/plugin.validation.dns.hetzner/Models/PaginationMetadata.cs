using System.Text.Json.Serialization;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns.Models;

internal sealed class PaginationMetadata
{
    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; init; }

    [JsonPropertyName("previous_page")]
    public int PreviousPage { get; init; }

    [JsonPropertyName("next_page")]
    public int NextPage { get; init; }

    [JsonPropertyName("last_page")]
    public int LastPage { get; init; }

    [JsonPropertyName("total_entries")]
    public int TotalEntries { get; init; }
}