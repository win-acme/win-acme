using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Godaddy
{

    public class DnsManagementClient
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly ILogService _log;
        readonly IProxyService _proxyService;
        private readonly string uri = "https://api.godaddy.com/";

        public DnsManagementClient(string apiKey, string apiSecret, ILogService logService, IProxyService proxyService)
        {
            _apiKey = apiKey;
            _apiSecret = apiSecret;
            _log = logService;
            _proxyService = proxyService;
        }

        public async Task CreateRecord(string domain, string identifier, RecordType type, string value)
        {
            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(_apiSecret))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"sso-key {_apiKey}:{_apiSecret}");
                } 
                else
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"sso-key {_apiKey}");
                }
                var putData = new List<object>() { new { ttl = 0, data = value } };
                var serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(putData);

                //Record successfully created
                // Wrap our JSON inside a StringContent which then can be used by the HttpClient class
                var typeTxt = type.ToString();
                var httpContent = new StringContent(serializedObject, Encoding.UTF8, "application/json");
                var buildApiUrl = $"v1/domains/{domain}/records/{typeTxt}/{identifier}";

                _log.Information("Godaddy API with: {0}", buildApiUrl);
                _log.Verbose("Godaddy Data with: {0}", serializedObject);

                var response = await client.PutAsync(buildApiUrl, httpContent);
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //_logService.Information("Godaddy Created Responded with: {0}", content);
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

        public async Task DeleteRecord(string domain, string identifier, RecordType type)
        {
            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrWhiteSpace(_apiSecret))
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"sso-key {_apiKey}:{_apiSecret}");
                }
                else
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"sso-key {_apiKey}");
                }
                var typeTxt = type.ToString();
                var buildApiUrl = $"v1/domains/{domain}/records/{typeTxt}/{identifier}";

                _log.Information("Godaddy API with: {0}", buildApiUrl); ;

                var response = await client.DeleteAsync(buildApiUrl);
                if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //_logService.Information("Godaddy Delete Responded with: {0}", content);
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
    }
}