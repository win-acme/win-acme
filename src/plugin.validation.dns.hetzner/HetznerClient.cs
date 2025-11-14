using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using PKISharp.WACS.Plugins.ValidationPlugins.Dns.Models;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns;

internal sealed class HetznerClient : IDisposable
{
    private static readonly Uri BASE_ADDRESS = new Uri("https://dns.hetzner.com/api/v1/");

    private ILogService _log;

    private HttpClient _httpClient;

    public HetznerClient(string apiToken, ILogService logService, IProxyService proxyService)
    {
        _log = logService;

        _httpClient = proxyService.GetHttpClient();
        _httpClient.BaseAddress = BASE_ADDRESS;
        _httpClient.DefaultRequestHeaders.Add("Auth-API-Token", apiToken);
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    public async Task<ICollection<ZoneDetails>> GetAllZonesAsync()
    {
        var zonesResponse = await _httpClient.GetFromJsonAsync<ZonesResponse>("zones").ConfigureAwait(false);
        if (zonesResponse is null)
        {
            _log.Warning("No zones found in Hetzner DNS");
            return Array.Empty<ZoneDetails>();
        }

        // Is only one page returned?
        if (zonesResponse.Meta.Pagination.LastPage == zonesResponse.Meta.Pagination.Page)
        {
            return zonesResponse.Zones;
        }

        var allZones = new List<ZoneDetails>();
        allZones.AddRange(zonesResponse.Zones);

        // As long as we are not on the last page continue
        while (zonesResponse.Meta.Pagination.LastPage != zonesResponse.Meta.Pagination.Page)
        {
            var nextPage = zonesResponse.Meta.Pagination.Page + 1;

            zonesResponse = await _httpClient.GetFromJsonAsync<ZonesResponse>($"zones?page={nextPage}").ConfigureAwait(false);
            if (zonesResponse is null)
            {
                _log.Warning($"No zones found on page {nextPage} in Hetzner DNS");
                return allZones;
            }

            allZones.AddRange(zonesResponse.Zones);
        }

        return allZones;
    }

    public async Task<ZoneDetails?> GetZoneAsync(string zoneId)
    {
        var zoneResponse = await _httpClient.GetStringAsync($"zones/{zoneId}").ConfigureAwait(false);
        
        ZoneDetails? zone = null;

        try
        {
            zone = JsonSerializer.Deserialize<ZoneDetails>(zoneResponse);
        }
        catch (JsonException ex)
        {
            _log.Warning($"Error parsing Zone: {ex}");
        }
        
        zone ??= JsonSerializer.Deserialize<ZoneResponse>(zoneResponse)?.Zone;

        if (zone is not null) return zone;
        
        _log.Warning($"Zone with zone id {zoneId} not found in Hetzner DNS");
        return null;

    }

    public async Task<bool> CreateRecordAsync(Record record)
    {
        using var response = await _httpClient.PostAsJsonAsync("records", record).ConfigureAwait(false);

        return response.IsSuccessStatusCode;
    }

    public async Task DeleteRecordAsync(Record record)
    {
        var recordSet = await _httpClient.GetFromJsonAsync<RecordResultSet>($"records?zone_id={record.zone_id}");
        if (recordSet is null || recordSet.records is null || recordSet.records.Length == 0)
        {
            _log.Warning($"Record set for zone id {record.zone_id} is empty");
            return;
        }

        var dnsRecord = recordSet.records
            .Where(x => x.zone_id == record.zone_id && x.name == record.name && x.value == record.value)
            .FirstOrDefault();

        if (dnsRecord is null)
        {
            _log.Warning($"DNS TXT record in zone {record.zone_id} not found");
            return;
        }

        using var deleteResponse = await _httpClient.DeleteAsync($"records/{dnsRecord.id}");

        deleteResponse.EnsureSuccessStatusCode();
    }

    private record RecordResultSet(RecordResult[] records);

    private record RecordResult(string type, string id, string created, string modified, string zone_id, string name, string value, int ttl);
}
