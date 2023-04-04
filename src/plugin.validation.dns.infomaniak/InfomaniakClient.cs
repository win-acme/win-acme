using PKISharp.WACS.Services;
using System.Net.Http.Headers;
using System.Net.Http;
using System;
using System.Linq;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PKISharp.WACS.Plugins.ValidationPlugins.Models;

namespace PKISharp.WACS.Plugins.ValidationPlugins;

internal class InfomaniakClient
{
    private readonly ILogService _log;
    private readonly HttpClient _httpClient;
    private const string Root = "https://api.infomaniak.com/";

    public InfomaniakClient(string apiToken, ILogService logService, IProxyService proxyService)
    {
        _log = logService;
        _httpClient = proxyService.GetHttpClient();
        _httpClient.BaseAddress = new Uri(Root);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiToken}");
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
    }
    public async Task<int> GetDomainId(string domain)
    {
        var apiUrl = $"/1/product?service_name=domain&customer_name={domain}";
        var response = await _httpClient.GetAsync(apiUrl);
        var json = await response.Content.ReadAsStringAsync();
        var domainList = JsonConvert.DeserializeObject<DomainListResponse>(json);

        var domainId = domainList?.Data?.FirstOrDefault(x => x.CustomerName == domain)?.Id ?? 0;
        if (domainId == 0)
        {
            _log.Error($"Infomaniak did not return record for domain: {domain}");
        }

        _log.Information($"Infomaniak found Domain ID: {domainId}");
        return domainId;
    }

    public async Task<int> CreateRecord(int domainId, string identifier, string value, int ttl = 300)
    {
        if (domainId <= 0) throw new ArgumentOutOfRangeException(nameof(domainId));
        if (ttl <= 0) throw new ArgumentOutOfRangeException(nameof(ttl));

        var apiUrl = $"/1/domain/{domainId}/dns/record";
        var postData = new DomainRecordCreateRequest
        {
            Type = "TXT",
            Source = identifier,
            Target = value,
            TimeToLive = ttl
        };
        var jsonContent = JsonConvert.SerializeObject(postData);
        var response = await _httpClient.PostAsync(apiUrl, new StringContent(jsonContent, Encoding.UTF8, MediaTypeNames.Application.Json));
        if (!response.IsSuccessStatusCode)
        {
            _log.Error("Infomaniak did not create record. Request Failure.");
            return 0;
        }

        //Get new Record ID - used for deleting
        var jsonResponse = await response.Content.ReadAsStringAsync();
        var createResponse = JsonConvert.DeserializeObject<DomainRecordCreateResponse>(jsonResponse);
        var id = (createResponse?.Data ?? 0);
        if (id == 0)
        {
            _log.Error($"Infomaniak did not create record: {jsonResponse}");
            return 0;
        }
        _log.Information($"Infomaniak created TXT Record with ID: {id}");

        return id;
    }

    public async Task DeleteRecord(int domainId, int recordId)
    {
        if (domainId <= 0) throw new ArgumentOutOfRangeException(nameof(domainId));
        if (recordId <= 0) throw new ArgumentOutOfRangeException(nameof(recordId));

        var apiUrl = $"/1/domain/{domainId}/dns/record/{recordId}";
        var response = await _httpClient.DeleteAsync(apiUrl);
        if (response.IsSuccessStatusCode)
        {
            _log.Information("Infomaniak successfully deleted TXT Record");
            return;
        }
        
        var content = await response.Content.ReadAsStringAsync();
        _log.Error($"Infomaniak did not delete record. Request Failure : {content}");
        response.EnsureSuccessStatusCode();
    }
}