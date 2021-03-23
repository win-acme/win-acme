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
        private readonly ILogService _logService;
        readonly ProxyService _proxyService;
        private readonly string uri = "https://api.godaddy.com/";

        public DnsManagementClient(string apiKey, ILogService logService, ProxyService proxyService)
        {
            _apiKey = apiKey;
            _logService = logService;
            _proxyService = proxyService;
        }

        public async Task CreateRecord(string domain, string identifier, RecordType type, string value)
        {
            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"sso-key {_apiKey}");

                var putData = new List<object>() { new { name = identifier, ttl = 3600, data = value } };

                string serializedObject = Newtonsoft.Json.JsonConvert.SerializeObject(putData);

                //Record successfully created
                // Wrap our JSON inside a StringContent which then can be used by the HttpClient class
                var typeTxt = type.ToString();
                var httpContent = new StringContent(serializedObject, Encoding.UTF8, "application/json");
                var buildApiUrl = $"v1/domains/{domain}/records/{typeTxt}";

                _logService.Information("Godaddy API with: {0}", buildApiUrl);
                _logService.Information("Godaddy Data with: {0}", serializedObject);

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

        public async Task DeleteRecord(string record, string identifier, RecordType type, string value)
        {
            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("Authorization", $"sso-key {_apiKey}");

                var typeTxt = type.ToString();
                var buildApiUrl = $"v1/domains/{identifier}/records/{typeTxt}/_acme-challenge";

                _logService.Information("Godaddy API with: {0}", buildApiUrl); ;

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