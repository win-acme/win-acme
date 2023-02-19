using PKISharp.WACS.Services;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Linode
{
    public class DnsManagementClient
    {
        private readonly ILogService _log;
        private readonly HttpClient _httpClient;
        private readonly string uri = "https://api.linode.com/";

        public DnsManagementClient(string apiToken, ILogService logService, IProxyService proxyService)
        {
            _log = logService;
            _httpClient = proxyService.GetHttpClient();
            _httpClient.BaseAddress = new Uri(uri);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<int> GetDomainId(string domain)
        {
            var apiUrl = $"v4/domains";
            var response = await _httpClient.GetAsync(apiUrl);
            var json = await response.Content.ReadAsStringAsync();
            var domainList = Newtonsoft.Json.JsonConvert.DeserializeObject<DomainListResponse>(json);

            var domainId = domainList?.data?.FirstOrDefault(x => x.domain == domain)?.id ?? 0;
            if (domainId == 0)
            {
                _log.Error($"Linode did not return record on first page for domain: {domain}");
            }

            _log.Information($"Linode found Domain ID: {domainId}");
            return domainId;
        }

        public async Task<int> CreateRecord(int domainId, string identifier, string value)
        {
            if (domainId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(domainId));
            }
            var apiUrl = $"v4/domains/{domainId}/records";
            var postData = new
            {
                type = "TXT",
                name = identifier,
                target = value,
                ttl_sec = 1
            };
            var jsonContent = JsonSerializer.Serialize(postData);
            var response = await _httpClient.PostAsync(apiUrl, new StringContent(jsonContent, Encoding.UTF8, "application/json"));
            if (!response.IsSuccessStatusCode)
            {
                _log.Error($"Linode did not create record. Request Failure.");
                return 0;
            }

            //Get new Record ID - used for deleting
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var createResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<DomainRecordCreateResponse>(jsonResponse);
            if ((createResponse?.id ?? 0) == 0)
            {
                _log.Error($"Linode did not create record: {jsonResponse}");
                return 0;
            }
            _log.Information($"Linode created TXT Record with ID: {createResponse?.id}");
            return createResponse?.id ?? 0;
        }

        public async Task<bool> DeleteRecord(int domainId, int recordId)
        {
            if (domainId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(domainId));
            }

            if (recordId <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(recordId));
            }
            var apiUrl = $"v4/domains/{domainId}/records/{recordId}";
            var response = await _httpClient.DeleteAsync(apiUrl);
            if (!response.IsSuccessStatusCode)
            {
                _log.Error($"Linode did not delete record. Request Failure.");
                return false;
            }
            _log.Information($"Linode successfully deleted TXT Record");
            return true;
        }
    }
}