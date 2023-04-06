using PKISharp.WACS.Clients.Acme;
using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Text.Json;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    internal class NetworkCheckService
    {
        private readonly IProxyService _proxyService;
        private readonly ILogService _log;
        private readonly ISettingsService _settings;

        public NetworkCheckService(IProxyService proxy, ISettingsService settings, ILogService log)
        {
            _proxyService = proxy;
            _settings = settings;
            _log = log;
        }

        /// <summary>
        /// Test the network connection
        /// </summary>
        internal async Task CheckNetwork()
        {
            using var httpClient = _proxyService.GetHttpClient();
            httpClient.BaseAddress = _settings.BaseUri;
            httpClient.Timeout = new TimeSpan(0, 0, 10);
            var success = await CheckNetworkUrl(httpClient, "directory");
            if (!success)
            {
                success = await CheckNetworkUrl(httpClient, "");
            }
            if (!success)
            {
                _log.Debug("Initial connection failed, retrying with TLS 1.2 forced");
                _proxyService.SslProtocols = SslProtocols.Tls12;
                success = await CheckNetworkUrl(httpClient, "directory");
                if (!success)
                {
                    success = await CheckNetworkUrl(httpClient, "");
                }
            }
            if (success)
            {
                _log.Information("Connection OK!");
            }
            else
            {
                _log.Warning("Initial connection failed");
            }
        }

        /// <summary>
        /// Test the network connection
        /// </summary>
        private async Task<bool> CheckNetworkUrl(HttpClient httpClient, string path)
        {
            try
            {
                var response = await httpClient.GetAsync(path).ConfigureAwait(false);
                await CheckNetworkResponse(response);
                return true;
            }
            catch (Exception ex)
            {
                _log.Debug($"Connection failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Deep inspection of initial response
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async static Task CheckNetworkResponse(HttpResponseMessage response)
        {
            if (response == null)
            {
                throw new Exception($"Server returned emtpy response");
            }
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Server returned status {response.StatusCode}:{response.ReasonPhrase}");
            }
            string? content;
            try
            {
                content = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to get response content", ex);
            }
            try
            {
                JsonSerializer.Deserialize(content, AcmeClientJson.Insensitive.ServiceDirectory);
            }
            catch (Exception ex)
            {
                throw new Exception($"Unable to parse response content", ex);
            }
        }
    }
}
