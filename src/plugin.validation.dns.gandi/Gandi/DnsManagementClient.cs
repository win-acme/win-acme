using Newtonsoft.Json;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Gandi
{
    /// <summary>
    /// Minimal client for the Gandi LiveDNS API (https://api.gandi.net/docs/livedns/)
    /// </summary>
    public class DnsManagementClient
    {
        private readonly string _apiToken;
        private readonly ILogService _log;
        private readonly IProxyService _proxyService;
        private const string _baseUri = "https://api.gandi.net/v5/livedns/";

        public DnsManagementClient(string apiToken, ILogService logService, IProxyService proxyService)
        {
            _apiToken = apiToken;
            _log = logService;
            _proxyService = proxyService;
        }

        private HttpClient CreateClient()
        {
            var client = _proxyService.GetHttpClient();
            client.BaseAddress = new Uri(_baseUri);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            // Personal Access Tokens use the Bearer scheme; legacy API keys
            // are prefixed with "Apikey". A PAT is always longer than 24 chars.
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiToken}");
            return client;
        }

        /// <summary>
        /// List all domains available in the account
        /// </summary>
        public async Task<IReadOnlyList<string>> GetDomains()
        {
            using var client = CreateClient();
            var domains = new List<string>();
            var page = 1;
            while (true)
            {
                var response = await client.GetAsync($"domains?per_page=100&page={page}");
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Unable to list Gandi domains ({(int)response.StatusCode}): {error}");
                }
                var content = await response.Content.ReadAsStringAsync();
                var parsed = JsonConvert.DeserializeObject<List<DomainModel>>(content) ?? new List<DomainModel>();
                if (parsed.Count == 0)
                {
                    break;
                }
                domains.AddRange(parsed.Where(x => !string.IsNullOrEmpty(x.Fqdn)).Select(x => x.Fqdn!));
                if (parsed.Count < 100)
                {
                    break;
                }
                page++;
            }
            return domains;
        }

        /// <summary>
        /// Get the current values of a TXT record set, or an empty list when it does not exist
        /// </summary>
        public async Task<List<string>> GetTxtValues(string fqdn, string recordName)
        {
            using var client = CreateClient();
            var response = await client.GetAsync($"domains/{fqdn}/records/{recordName}/TXT");
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new List<string>();
            }
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Unable to read Gandi record {recordName} TXT ({(int)response.StatusCode}): {error}");
            }
            var content = await response.Content.ReadAsStringAsync();
            var parsed = JsonConvert.DeserializeObject<RecordModel>(content);
            return parsed?.RrsetValues?.ToList() ?? new List<string>();
        }

        /// <summary>
        /// Replace the TXT record set with the given values
        /// </summary>
        public async Task PutTxtValues(string fqdn, string recordName, IEnumerable<string> values)
        {
            using var client = CreateClient();
            var payload = new RecordModel
            {
                RrsetValues = values.ToArray(),
                RrsetTtl = 300
            };
            var serialized = JsonConvert.SerializeObject(payload);
            var httpContent = new StringContent(serialized, Encoding.UTF8, "application/json");
            var response = await client.PutAsync($"domains/{fqdn}/records/{recordName}/TXT", httpContent);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Unable to create Gandi record {recordName} TXT ({(int)response.StatusCode}): {error}");
            }
        }

        /// <summary>
        /// Delete the TXT record set entirely
        /// </summary>
        public async Task DeleteTxtRecord(string fqdn, string recordName)
        {
            using var client = CreateClient();
            var response = await client.DeleteAsync($"domains/{fqdn}/records/{recordName}/TXT");
            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Unable to delete Gandi record {recordName} TXT ({(int)response.StatusCode}): {error}");
            }
        }

        private class DomainModel
        {
            [JsonProperty("fqdn")]
            public string? Fqdn { get; set; }
        }

        private class RecordModel
        {
            [JsonProperty("rrset_values")]
            public string[]? RrsetValues { get; set; }

            [JsonProperty("rrset_ttl", NullValueHandling = NullValueHandling.Ignore)]
            public int RrsetTtl { get; set; }
        }
    }
}
