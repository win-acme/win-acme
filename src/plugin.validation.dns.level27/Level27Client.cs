using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dns
{
    internal sealed class Level27Client : IDisposable
    {
        private const string DefaultBaseAddress = "https://api.level27.eu/v1/";

        private readonly ILogService _log;
        private readonly HttpClient _httpClient;

        public Level27Client(string apiKey, string? apiUrl, ILogService log, IProxyService proxyService)
        {
            _log = log;

            _httpClient = proxyService.GetHttpClient();
            _httpClient.BaseAddress = new Uri(NormalizeBaseAddress(apiUrl));
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        }

        private static string NormalizeBaseAddress(string? apiUrl)
        {
            var url = string.IsNullOrWhiteSpace(apiUrl) ? DefaultBaseAddress : apiUrl.Trim();
            if (!url.EndsWith("/", StringComparison.Ordinal))
            {
                url += "/";
            }
            return url;
        }

        public async Task<Level27Domain?> FindDomainAsync(string recordName)
        {
            var labels = recordName.Split('.');
            for (var i = 0; i < labels.Length - 1; i++)
            {
                var candidate = string.Join('.', labels[i..]);
                var response = await _httpClient.GetFromJsonAsync<DomainsResponse>($"domains?filter={Uri.EscapeDataString(candidate)}").ConfigureAwait(false);
                if (response?.Domains == null)
                {
                    continue;
                }
                foreach (var domain in response.Domains)
                {
                    if (string.Equals(domain.Fullname, candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return domain;
                    }
                }
            }
            return null;
        }

        public async Task<int?> CreateRecordAsync(int domainId, string name, string content)
        {
            var request = new RecordRequest(name, "TXT", content);
            using var response = await _httpClient.PostAsJsonAsync($"domains/{domainId}/records", request).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _log.Error("Level27 API returned {StatusCode} while creating the TXT record.", (int)response.StatusCode);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<RecordResponse>().ConfigureAwait(false);
            return result?.Record?.Id;
        }

        public async Task DeleteRecordAsync(int domainId, int recordId)
        {
            using var response = await _httpClient.DeleteAsync($"domains/{domainId}/records/{recordId}").ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        public void Dispose() => _httpClient.Dispose();

        internal sealed class DomainsResponse
        {
            [JsonPropertyName("domains")]
            public List<Level27Domain>? Domains { get; set; }
        }

        internal sealed class RecordResponse
        {
            [JsonPropertyName("record")]
            public Level27Record? Record { get; set; }
        }

        internal sealed record RecordRequest(
            [property: JsonPropertyName("name")] string Name,
            [property: JsonPropertyName("type")] string Type,
            [property: JsonPropertyName("content")] string Content);
    }

    internal sealed class Level27Domain
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("fullname")]
        public string Fullname { get; set; } = string.Empty;
    }

    internal sealed class Level27Record
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
    }
}
