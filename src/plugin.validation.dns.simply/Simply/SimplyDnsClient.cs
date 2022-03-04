using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    public class SimplyDnsClient
    {
        private readonly ILogService _log;
        private readonly HttpClient _httpClient = new();
        private readonly string _baseUrl = "https://api.simply.com/2";

        public SimplyDnsClient(string account, string apiKey, ILogService log)
        {
            _log = log;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", EncodeBasicAuth(account, apiKey));
        }

        public async Task CreateRecordAsync(string identifier, string domain, string value)
        {
            var objectId = GetObjectId(identifier);
            var records = await GetRecordsAsync(objectId);

            // Remove any dangling records
            var existingRecords = records.Where(x => x.Type == "TXT" && x.GetHostname(objectId) == domain).ToList();
            foreach (var record in existingRecords)
            {
                await DeleteRecordAsync(objectId, record.RecordId);
            }

            await CreateRecordAsync(objectId, new DnsRecord
            {
                Type = "TXT",
                Name = domain,
                Data = value,
                Ttl = 3600,
            });
        }

        public async Task DeleteRecordAsync(string identifier, string domain, string value)
        {
            var objectId = GetObjectId(identifier);
            var records = await GetRecordsAsync(objectId);
            var record = records.SingleOrDefault(x => x.Type == "TXT" && x.GetHostname(objectId) == domain && x.Data == value);
            if (record is null)
            {
                _log.Warning($"The TXT record {domain} that should be deleted does not exist at Simply.");
                return;
            }

            await DeleteRecordAsync(objectId, record.RecordId);
        }

        private async Task CreateRecordAsync(string objectId, DnsRecord record)
        {
            using var content = new StringContent(JsonSerializer.Serialize(record), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_baseUrl + $"/my/products/{WebUtility.UrlEncode(objectId)}/dns/records", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            _log.Information("Simply responded with status {0}: {1}", (int)response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
        }

        private async Task DeleteRecordAsync(string objectId, int recordId)
        {
            using var response = await _httpClient.DeleteAsync(_baseUrl + $"/my/products/{WebUtility.UrlEncode(objectId)}/dns/records/{recordId}");
            var responseBody = await response.Content.ReadAsStringAsync();
            _log.Information("Simply responded with status {0}: {1}", (int)response.StatusCode, responseBody);
            response.EnsureSuccessStatusCode();
        }

        private async Task<List<DnsRecord>> GetRecordsAsync(string objectId)
        {
            using var response = await _httpClient.GetAsync(_baseUrl + $"/my/products/{WebUtility.UrlEncode(objectId)}/dns/records");
            _log.Information("Simply responded with status {0}", (int)response.StatusCode);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            var records = await JsonSerializer.DeserializeAsync<DnsRecordList>(stream);
            return records!.Records;
        }


        private static string EncodeBasicAuth(string account, string apiKey)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(WebUtility.UrlEncode(account) + ":" + WebUtility.UrlEncode(apiKey)));
        }

        private static string GetObjectId(string identifier)
        {
            var parts = identifier.Split('.');
            return parts[^2] + "." + parts[^1];
        }
    }
}