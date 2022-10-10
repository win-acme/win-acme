using DnsClient.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Simply
{
    public class SimplyDnsClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl = "https://api.simply.com/2";

        public SimplyDnsClient(string account, string apiKey, HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", EncodeBasicAuth(account, apiKey));
        }

        public async Task<List<Product>> GetAllProducts()
        {
            var products = await GetProductsAsync();
            return products;
        }

        public async Task CreateRecordAsync(string objectId, string domain, string value)
        {
            await CreateRecordAsync(objectId, new DnsRecord
            {
                Type = "TXT",
                Name = domain,
                Data = value,
                Ttl = 3600,
            });
        }

        public async Task DeleteRecordAsync(string objectId, string domain, string value)
        {
            var products = await GetProductsAsync();
            var product = products.FirstOrDefault(x => x.Object == objectId);
            if (product == null)
            {
                throw new Exception($"Unable to find product with object id {objectId}.");
            }
            var records = await GetRecordsAsync(objectId);
            var record = records.SingleOrDefault(x => x.Type == "TXT" && $"{x.Name}.{product.Domain?.NameIdn ?? "unknown"}" == domain && x.Data == value);
            if (record is null)
            {
                throw new Exception($"The TXT record {domain} that should be deleted does not exist at Simply.");
            }

            await DeleteRecordAsync(objectId, record.RecordId);
        }

        private async Task<List<Product>> GetProductsAsync()
        {
            using var response = await _httpClient.GetAsync(_baseUrl + "/my/products");
            await using var stream = await response.Content.ReadAsStreamAsync();
            var products = await JsonSerializer.DeserializeAsync<ProductList>(stream);
            if (products == null || products.Products == null)
            {
                throw new InvalidOperationException();
            }
            return products.Products;
        }

        private async Task CreateRecordAsync(string objectId, DnsRecord record)
        {
            using var content = new StringContent(JsonSerializer.Serialize(record), Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_baseUrl + $"/my/products/{WebUtility.UrlEncode(objectId)}/dns/records", content);
            var responseBody = await response.Content.ReadAsStringAsync();
            _ = response.EnsureSuccessStatusCode();
        }

        private async Task DeleteRecordAsync(string objectId, int recordId)
        {
            using var response = await _httpClient.DeleteAsync(_baseUrl + $"/my/products/{WebUtility.UrlEncode(objectId)}/dns/records/{recordId}");
            var responseBody = await response.Content.ReadAsStringAsync();
            _ = response.EnsureSuccessStatusCode();
        }

        private async Task<List<DnsRecord>> GetRecordsAsync(string objectId)
        {
            using var response = await _httpClient.GetAsync(_baseUrl + $"/my/products/{WebUtility.UrlEncode(objectId)}/dns/records");
            _ = response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync();
            var records = await JsonSerializer.DeserializeAsync<DnsRecordList>(stream);
            if (records == null || records.Records == null)
            {
                throw new InvalidOperationException();
            }
            return records.Records;
        }

        private static string EncodeBasicAuth(string account, string apiKey)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(WebUtility.UrlEncode(account) + ":" + WebUtility.UrlEncode(apiKey)));
        }
    }
}