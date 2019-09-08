using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dreamhost
{
    public class DnsManagementClient
    {
        private readonly string _apiKey;
        private readonly ILogService _logService;
        private readonly string uri = "https://api.dreamhost.com/";

        public DnsManagementClient(string apiKey, ILogService logService)
        {
            _apiKey = apiKey;
            _logService = logService;
        }

        public Task CreateRecord(string record, RecordType type, string value)
        {
            var response = SendRequest("dns-add_record",
                new Dictionary<string, string>
                {
                    {"record", record},
                    {"type", type.ToString()},
                    {"value", value}
                });

            _logService.Information("Dreamhost Responded with: {0}", response.Content.ReadAsStringAsync().Result);
            _logService.Information("Waiting for 30 seconds");
            Thread.Sleep(TimeSpan.FromSeconds(30));
            return Task.CompletedTask;
        }

        public void DeleteRecord(string record, RecordType type, string value)
        {
            var args = new Dictionary<string, string>
            {
                {"record", record},
                {"type", type.ToString()},
                {"value", value}
            };

            var response = SendRequest("dns-remove_record", args);

            _logService.Information("Dreamhost Responded with: {0}", response.Content.ReadAsStringAsync().Result);
            _logService.Information("Waiting for 30 seconds");

            Thread.Sleep(TimeSpan.FromSeconds(30));
        }

        private HttpResponseMessage SendRequest(string command, IEnumerable<KeyValuePair<string, string>> args)
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(uri)
            };

            var queryString = System.Web.HttpUtility.ParseQueryString(string.Empty);
            queryString.Add("key", _apiKey);
            queryString.Add("unique_id", Guid.NewGuid().ToString());
            queryString.Add("format", "json");
            queryString.Add("cmd", command);

            foreach (var arg in args)
            {
                queryString.Add(arg.Key, arg.Value);
            }

            var response = client.GetAsync("?" + queryString).Result;
            return response;
        }
    }
}
