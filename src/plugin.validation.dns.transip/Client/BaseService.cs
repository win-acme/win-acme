using Newtonsoft.Json;
using PKISharp.WACS.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace TransIp.Library
{
    public abstract class BaseService
    {
        private readonly HttpClient _client;
        private readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0).ToLocalTime();

        public BaseService(IProxyService proxyService)
        {
            _client = proxyService.GetHttpClient();
            _client.BaseAddress = new Uri("https://api.transip.nl/v6/");
        }

        protected internal virtual Task<HttpClient> GetClient() => Task.FromResult(_client);

        protected async Task<TransIpResponse<TResponse>> Get<TResponse>(string url) =>
            new TransIpResponse<TResponse>(await SendContent(url, HttpMethod.Get));

        protected async Task<TransIpResponse<TResponse>> Post<TRequest, TResponse>(string url, TRequest payload) =>
            new TransIpResponse<TResponse>(await Send(url, HttpMethod.Post, payload));
        protected async Task<TransIpResponse> Post<TRequest>(string url, TRequest payload) =>
            await Send(url, HttpMethod.Post, payload);

        protected async Task<TransIpResponse> Delete<TRequest>(string url, TRequest payload) => 
            await Send(url, HttpMethod.Delete, payload);

        protected async Task<TransIpResponse> Patch<TRequest>(string url, TRequest payload) => 
            await Send(url, new HttpMethod("PATCH"), payload);

        protected async Task<TransIpResponse> Send<TRequest>(string url, HttpMethod method, TRequest payload)
        {
            var body = JsonConvert.SerializeObject(payload);
            var content = new StringContent(body);
            return await SendContent(url, method, content);
        }

        protected async Task<TransIpResponse> SendContent(string url, HttpMethod method, HttpContent? content = null)
        {
            var client = await GetClient();
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url, UriKind.Relative),
                Content = content,
                Method = method
            };
            var httpResponse = await client.SendAsync(request);
            var parsed = await ParseResponse(httpResponse);
            if (parsed.Success == false)
            {
                throw new Exception(parsed.Payload);
            }
            return parsed;
        } 

        protected async Task<TransIpResponse> ParseResponse(HttpResponseMessage response, TransIpResponse? output = null)
        {
            if (output == null)
            {
                output = new TransIpResponse();
            }
            output.Success = response.IsSuccessStatusCode;
            output.Payload = await response.Content.ReadAsStringAsync();
            if (!output.Success)
            {
                try
                {
                    var error = JsonConvert.DeserializeObject<TransIpError>(output.Payload);
                    output.Payload = error?.Error;
                } 
                catch { }
            }
            if (response.Headers.Contains("X-Rate-Limit-Remaining"))
            {
                output.RateLimitRemaining = int.Parse(response.Headers.GetValues("X-Rate-Limit-Remaining").FirstOrDefault() ?? "0");
            } 
            if (response.Headers.Contains("X-Rate-Limit-Reset"))
            {
                output.RateLimitReset = _epoch.AddSeconds(double.Parse(response.Headers.GetValues("X-Rate-Limit-Reset").FirstOrDefault() ?? "0"));
            }
            return output;
        }

        public class TransIpError
        {
            public string? Error { get; set; }
        }

        public class TransIpResponse
        {
            public bool Success { get; set; }
            public string? Payload { get; set; }
            public int RateLimitRemaining { get; set; }
            public DateTime RateLimitReset { get; set; }
        }

        public class TransIpResponse<T> : TransIpResponse
        {
            public TransIpResponse(TransIpResponse original) 
            {
                Success = original.Success;
                Payload = original.Payload;
                RateLimitRemaining = original.RateLimitRemaining;
                RateLimitReset = original.RateLimitReset;
                PayloadTyped = JsonConvert.DeserializeObject<T>(original.Payload ?? "");
            }

            public T? PayloadTyped { get; set; }
        }
    }
}
