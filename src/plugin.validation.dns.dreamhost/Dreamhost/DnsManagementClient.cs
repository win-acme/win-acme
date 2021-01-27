using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
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

        public async Task CreateRecord(string record, RecordType type, string value)
        {
            var response = await SendRequest("dns-add_record",
                new Dictionary<string, string>
                {
                    {"record", record},
                    {"type", type.ToString()},
                    {"value", value}
                });
            var content = await response.Content.ReadAsStringAsync();
            _logService.Information("Dreamhost Responded with: {0}", content);
            _logService.Information("Waiting for 30 seconds");
            await Task.Delay(30000);
        }

        public async Task DeleteRecord(string record, RecordType type, string value)
        {
            var args = new Dictionary<string, string>
            {
                {"record", record},
                {"type", type.ToString()},
                {"value", value}
            };
            var response = await SendRequest("dns-remove_record", args);
            var content = await response.Content.ReadAsStringAsync();
            _logService.Information("Dreamhost Responded with: {0}", content);
            _logService.Information("Waiting for 30 seconds");
            await Task.Delay(30000);
        }

        private async Task<HttpResponseMessage> SendRequest(string command, IEnumerable<KeyValuePair<string, string>> args)
        {
            using (var client = new HttpClient { BaseAddress = new Uri(uri) })
            {
                var queryString = new Dictionary<string, string>
                {
                    { "key", _apiKey },
                    { "unique_id", Guid.NewGuid().ToString() },
                    { "format", "json" },
                    { "cmd", command }
                };
                foreach (var arg in args)
                {
                    queryString.Add(arg.Key, arg.Value);
                }
                return await client.GetAsync("?" + string.Join("&", queryString.Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}")));
            };
        }
    }
}