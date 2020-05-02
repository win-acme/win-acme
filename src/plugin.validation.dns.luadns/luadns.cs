using PKISharp.WACS.Clients.DNS;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class LuaDns : DnsValidation<LuaDns>
    {
        private class ZoneData
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }
        }

        private class RecordData
        {
            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("zone_id")]
            public int ZoneId { get; set; }

            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("type")]
            public string Type { get; set; }

            [JsonPropertyName("content")]
            public string Content { get; set; }

            [JsonPropertyName("ttl")]
            public int TTL { get; set; }
        }

        private static readonly Uri _LuaDnsApiEndpoint = new Uri("https://api.luadns.com/v1/", UriKind.Absolute);
        private static readonly Dictionary<string, RecordData> _recordsMap = new Dictionary<string, RecordData>();

        private readonly ProxyService _proxyService;

        private readonly string _userName;
        private readonly string _apiKey;

        public LuaDns(
            LookupClientProvider dnsClient,
            ProxyService proxy,
            ILogService log,
            ISettingsService settings,
            LuaDnsOptions options): base(dnsClient, log, settings)
        {
            _proxyService = proxy;
            _userName = options.Username;
            _apiKey = options.APIKey.Value;
        }

        public override async Task CreateRecord(string recordName, string token)
        {
            _log.Information("Creating LuaDNS verification record");

            using var client = GetClient();
            var response = await client.GetAsync(new Uri(_LuaDnsApiEndpoint, "zones"));
            if (!response.IsSuccessStatusCode)
            {
                _log.Information("Failed to get DNS zones list for account. Aborting.");
                return;
            }

            var payload = await response.Content.ReadAsStringAsync();
            var zones = JsonSerializer.Deserialize<ZoneData[]>(payload);
            var targetZone = zones.Where(d => recordName.EndsWith(d.Name, StringComparison.InvariantCultureIgnoreCase)).OrderByDescending(d => d.Name.Length).FirstOrDefault();
            if (targetZone == null)
            {
                _log.Information("No matching zone found in LuaDNS account. Aborting");
                return;
            }

            var newRecord = new RecordData { Name = $"{recordName}.", Type = "TXT", Content = token, TTL = 300 };
            payload = JsonSerializer.Serialize(newRecord);

            response = await client.PostAsync(new Uri(_LuaDnsApiEndpoint, $"zones/{targetZone.Id}/records"), new StringContent(payload, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                _log.Information("Failed to create DNS verification record");
                return;
            }

            payload = await response.Content.ReadAsStringAsync();
            newRecord = JsonSerializer.Deserialize<RecordData>(payload);

            _recordsMap[recordName] = newRecord;

            _log.Information("DNS Record created. Waiting 30 seconds to allow propagation.");
            await Task.Delay(30000);
        }

        public override async Task DeleteRecord(string recordName, string token)
        {
            if (!_recordsMap.ContainsKey(recordName))
            {
                _log.Information($"No record with name {recordName} was created");
                return;
            }

            _log.Information("Deleting LuaDNS verification record");

            using var client = GetClient();
            var response = await client.DeleteAsync(new Uri(_LuaDnsApiEndpoint, $"zones/{_recordsMap[recordName].ZoneId}/records/{_recordsMap[recordName].Id}"));
            if (!response.IsSuccessStatusCode)
            {
                _log.Information("Failed to delete DNS verification record");
                return;
            }

            _ = _recordsMap.Remove(recordName);
        }

        private HttpClient GetClient()
        {
            var client = _proxyService.GetHttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_userName}:{_apiKey}")));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return client;
        }
    }
}