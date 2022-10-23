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

namespace PKISharp.WACS.Plugins.ValidationPlugins.Dnsmadeeasy
{
    public class DnsManagementClient
    {
        private readonly string _apiKey;
        private readonly string _apiSecret;
        private readonly ILogService _log;
        readonly IProxyService _proxyService;
        private readonly string uri = "https://api.dnsmadeeasy.com/";

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
            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var currentDate = DateTime.UtcNow.ToString("r");
                var hash = SHA1Hash(currentDate, _apiSecret);
                client.DefaultRequestHeaders.Add("x-dnsme-apiKey", _apiKey);
                client.DefaultRequestHeaders.Add("x-dnsme-requestDate", currentDate);
                client.DefaultRequestHeaders.Add("x-dnsme-hmac", hash);

                var buildApiUrl = $"V2.0/dns/managed/name?domainname={domain}";
                _log.Information("Dnsmadeeasy API with: {0}", buildApiUrl);

                var response = await client.GetAsync(buildApiUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var request = JsonConvert.DeserializeObject<DomainResponse>(json);

                    if (request == null)
                        throw new ArgumentNullException("Unexpected null response for " + domain);

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
        }
        class DomainResponse
        {
            public string id { get; set; }
            public string name { get; set; }
            public string type { get; set; }
        }
        #endregion

        #region Lookup Domain Record Id
        public async Task<string[]> LookupDomainRecordId(string domainid, string recordName, RecordType type)
        {
            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var currentDate = DateTime.UtcNow.ToString("r");
                var hash = SHA1Hash(currentDate, _apiSecret);
                client.DefaultRequestHeaders.Add("x-dnsme-apiKey", _apiKey);
                client.DefaultRequestHeaders.Add("x-dnsme-requestDate", currentDate);
                client.DefaultRequestHeaders.Add("x-dnsme-hmac", hash);

                string typeTxt = type.ToString();
                var buildApiUrl = $"V2.0/dns/managed/{domainid}/records?recordName={recordName}&type={typeTxt}";
                _log.Information("Dnsmadeeasy API with: {0}", buildApiUrl);

                var response = await client.GetAsync(buildApiUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    var request = JsonConvert.DeserializeObject<DomainResponseCollection>(json);

                    if (request == null || request.data == null || request.data.Length == 0)
                        return Array.Empty<string>();

                    List<string> recordId = new List<string>();
                    foreach (var result in request.data)
                    {
                        if (string.Compare(result.name, recordName, true) == 0 && string.Compare(result.type, typeTxt, true) == 0)
                            recordId.Add(result.id);
                    }

                    return recordId.ToArray();
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    throw new Exception(content);
                }
            }
        }
        class DomainResponseCollection
        {
            public DomainRequest[] data { get; set; }
        }
        class DomainRequest : DomainResponse
        {
            public string id { get; set; }
        }
        #endregion

        public async Task CreateRecord(string domain, string identifier, RecordType type, string value)
        {
            string domainid = await LookupDomainId(domain);

            // Ensure any existing record are deleted.
            string[] domainRecordIds = await LookupDomainRecordId(domainid, identifier, type);
            if (domainRecordIds.Length > 0)
            {
                await DeleteRecord(domainid, domainRecordIds);
            }

            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var currentDate = DateTime.UtcNow.ToString("r");
                var hash = SHA1Hash(currentDate, _apiSecret);
                client.DefaultRequestHeaders.Add("x-dnsme-apiKey", _apiKey);
                client.DefaultRequestHeaders.Add("x-dnsme-requestDate", currentDate);
                client.DefaultRequestHeaders.Add("x-dnsme-hmac", hash);

                var putData = new { name = identifier, type = type.ToString(), value, ttl = 600, gtdLocation = "DEFAULT" };
                var serializedObject = JsonConvert.SerializeObject(putData);

                //Record successfully created
                // Wrap our JSON inside a StringContent which then can be used by the HttpClient class
                var httpContent = new StringContent(serializedObject, Encoding.UTF8, "application/json");
                var buildApiUrl = $"V2.0/dns/managed/{domainid}/records/";

                _log.Information("Dnsmadeeasy API with: {0}", buildApiUrl);
                _log.Verbose("Dnsmadeeasy Data with: {0}", serializedObject);

                var response = await client.PostAsync(buildApiUrl, httpContent);
                if (response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    //_logService.Information("Dnsmadeeasy Created Responded with: {0}", content);
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

        static string SHA1Hash(string input, string key)
        {
            using (var shaProvider = new HMACSHA1(new UTF8Encoding().GetBytes(key)))
            {
                return Hash(shaProvider, input);
            }
        }
        static string Hash(HashAlgorithm a, string input)
        {
            var bytes = a.ComputeHash(new UTF8Encoding().GetBytes(input));
            return ToHex(bytes);
        }
        static string ToHex(byte[] value)
        {
            var stringBuilder = new StringBuilder();
            if (value != null)
            {
                foreach (var b in value)
                {
                    stringBuilder.Append(HexStringTable[b]);
                }
            }

            return stringBuilder.ToString();
        }
        private static readonly string[] HexStringTable =
        {
            "00", "01", "02", "03", "04", "05", "06", "07", "08", "09", "0a", "0b", "0c", "0d", "0e", "0f",
            "10", "11", "12", "13", "14", "15", "16", "17", "18", "19", "1a", "1b", "1c", "1d", "1e", "1f",
            "20", "21", "22", "23", "24", "25", "26", "27", "28", "29", "2a", "2b", "2c", "2d", "2e", "2f",
            "30", "31", "32", "33", "34", "35", "36", "37", "38", "39", "3a", "3b", "3c", "3d", "3e", "3f",
            "40", "41", "42", "43", "44", "45", "46", "47", "48", "49", "4a", "4b", "4c", "4d", "4e", "4f",
            "50", "51", "52", "53", "54", "55", "56", "57", "58", "59", "5a", "5b", "5c", "5d", "5e", "5f",
            "60", "61", "62", "63", "64", "65", "66", "67", "68", "69", "6a", "6b", "6c", "6d", "6e", "6f",
            "70", "71", "72", "73", "74", "75", "76", "77", "78", "79", "7a", "7b", "7c", "7d", "7e", "7f",
            "80", "81", "82", "83", "84", "85", "86", "87", "88", "89", "8a", "8b", "8c", "8d", "8e", "8f",
            "90", "91", "92", "93", "94", "95", "96", "97", "98", "99", "9a", "9b", "9c", "9d", "9e", "9f",
            "a0", "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "aa", "ab", "ac", "ad", "ae", "af",
            "b0", "b1", "b2", "b3", "b4", "b5", "b6", "b7", "b8", "b9", "ba", "bb", "bc", "bd", "be", "bf",
            "c0", "c1", "c2", "c3", "c4", "c5", "c6", "c7", "c8", "c9", "ca", "cb", "cc", "cd", "ce", "cf",
            "d0", "d1", "d2", "d3", "d4", "d5", "d6", "d7", "d8", "d9", "da", "db", "dc", "dd", "de", "df",
            "e0", "e1", "e2", "e3", "e4", "e5", "e6", "e7", "e8", "e9", "ea", "eb", "ec", "ed", "ee", "ef",
            "f0", "f1", "f2", "f3", "f4", "f5", "f6", "f7", "f8", "f9", "fa", "fb", "fc", "fd", "fe", "ff"
        };
        public async Task DeleteRecord(string domain, string identifier, RecordType type)
        {
            string domainid = await LookupDomainId(domain);
            string[] domainRecordIds = await LookupDomainRecordId(domainid, identifier, type);

            await DeleteRecord(domainid, domainRecordIds);
        }
        public async Task DeleteRecord(string domainid, string[] domainRecordIds)
        {
            using (var client = _proxyService.GetHttpClient())
            {
                client.BaseAddress = new Uri(uri);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var currentDate = DateTime.UtcNow.ToString("r");
                var hash = SHA1Hash(currentDate, _apiSecret);
                client.DefaultRequestHeaders.Add("x-dnsme-apiKey", _apiKey);
                client.DefaultRequestHeaders.Add("x-dnsme-requestDate", currentDate);
                client.DefaultRequestHeaders.Add("x-dnsme-hmac", hash);

                foreach (var domainRecordId in domainRecordIds)
                {
                    var buildApiUrl = $"V2.0/dns/managed/{domainid}/records/{domainRecordId}";
                    _log.Information("Dnsmadeeasy API with: {0}", buildApiUrl);

                    var response = await client.DeleteAsync(buildApiUrl);
                    if (response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NoContent)
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
}