using Newtonsoft.Json;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.DnsMadeEasy
{
    public class DnsManagementClient
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly ILogService _log;
        readonly IProxyService _proxyService;
        private readonly string _uri = "https://api.dnsmadeeasy.com/";

        public DnsManagementClient(string apiKey, string apiSecret, ILogService logService, IProxyService proxyService)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _log = logService;
            _proxyService = proxyService;
        }

        #region Lookup Domain Id
        public async Task<string> LookupDomainId(string domain)
        {
            using var client = GetClient();
            var buildApiUrl = $"V2.0/dns/managed/name?domainname={domain}";

            var response = await client.GetAsync(buildApiUrl);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string json = await response.Content.ReadAsStringAsync();
                var request = JsonConvert.DeserializeObject<DomainResponse>(json);

                if (request == null || request.id == null)
                    throw new ArgumentNullException($"Unexpected null response for {domain}");

                if (string.Compare(request.name, domain, true) != 0)
                    throw new InvalidDataException($"Domain returned an unexpected result requested: {domain} != {request.name}");

                return request.id;
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception(content);
            }
        }
        class DomainResponse
        {
            public string? id { get; set; }
            public string? name { get; set; }
            public string? type { get; set; }
        }
        #endregion

        #region Lookup Domain Record Id
        public async Task<string[]> LookupDomainRecordId(string domainId, string recordName, RecordType type)
        {
            using var client = GetClient();
            string recordType = type.ToString();
            var buildApiUrl = $"V2.0/dns/managed/{domainId}/records?recordName={recordName}&type={recordType}";

            var response = await client.GetAsync(buildApiUrl);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                string json = await response.Content.ReadAsStringAsync();
                var request = JsonConvert.DeserializeObject<DomainResponseCollection>(json);

                if (request == null || request.data == null || request.data.Length == 0)
                    return Array.Empty<string>();

                List<string> recordId = new();
                foreach (var result in request.data)
                {
                    if (string.Compare(result.name, recordName, true) == 0 &&
                        string.Compare(result.type, recordType, true) == 0 &&
                        result.id != null)
                    {
                        recordId.Add(result.id);
                    }
                }

                return recordId.ToArray();
            }
            else
            {
                var content = await response.Content.ReadAsStringAsync();
                throw new Exception(content);
            }
        }
        class DomainResponseCollection
        {
            public DomainRequest[]? data { get; set; }
        }
        class DomainRequest : DomainResponse {}
        #endregion

        private HttpClient GetClient()
        {
            var client = _proxyService.GetHttpClient();
            client.BaseAddress = new Uri(_uri);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var currentDate = DateTime.UtcNow.ToString("r");
            client.DefaultRequestHeaders.Add("x-dnsme-apiKey", _apiKey);
            client.DefaultRequestHeaders.Add("x-dnsme-requestDate", currentDate);
            client.DefaultRequestHeaders.Add("x-dnsme-hmac", HMACSHA1(currentDate, _apiSecret));
            return client;
        }
        private string HMACSHA1(string text, string key)
        {
            using var hmacsha256 = new HMACSHA1(Encoding.UTF8.GetBytes(key));
            var hash = hmacsha256.ComputeHash(Encoding.UTF8.GetBytes(text));
            return BitConverter.ToString(hash).Replace("-", "").ToLower();
        }
        public async Task CreateRecord(string domain, string recordName, RecordType type, string value)
        {
            string domainId = await LookupDomainId(domain);

            // Ensure any existing record are deleted.
            string[] domainRecordIds = await LookupDomainRecordId(domainId, recordName, type);
            if (domainRecordIds.Length > 0)
            {
                await DeleteRecord(domainId, domainRecordIds);
            }

            using (var client = GetClient())
            {
                var putData = new { name = recordName, type = type.ToString(), value, ttl = 600, gtdLocation = "DEFAULT" };
                var serializedObject = JsonConvert.SerializeObject(putData);

                //Record successfully created
                // Wrap our JSON inside a StringContent which then can be used by the HttpClient class
                var httpContent = new StringContent(serializedObject, Encoding.UTF8, "application/json");
                var buildApiUrl = $"V2.0/dns/managed/{domainId}/records/";

                var response = await client.PostAsync(buildApiUrl, httpContent);
                if (response.StatusCode == HttpStatusCode.Created)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //_logService.Information("DnsMadeEasy Created Responded with: {0}", content);
                    //_logService.Information("Waiting for 30 seconds");
                    //await Task.Delay(30000);
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new Exception(content);
                }
            };
        }
        public async Task DeleteRecord(string domain, string recordName, RecordType type)
        {
            string domainId = await LookupDomainId(domain);
            string[] domainRecordIds = await LookupDomainRecordId(domainId, recordName, type);

            await DeleteRecord(domainId, domainRecordIds);
        }
        public async Task DeleteRecord(string domainId, string[] domainRecordIds)
        {
            using var client = GetClient();
            foreach (var domainRecordId in domainRecordIds)
            {
                var buildApiUrl = $"V2.0/dns/managed/{domainId}/records/{domainRecordId}";

                var response = await client.DeleteAsync(buildApiUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    var content = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new Exception(content);
                }
            }
        }
    }
}