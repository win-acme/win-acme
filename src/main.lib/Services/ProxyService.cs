using System;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace PKISharp.WACS.Services
{
    public class ProxyService : IProxyService
    {
        private readonly ILogService _log;
        private IWebProxy? _proxy;
        private readonly ISettingsService _settings;
        private readonly SecretServiceManager _secretService;
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

        public ProxyService(ILogService log, ISettingsService settings, SecretServiceManager secretService)
        {
            _log = log;
            _settings = settings;
            _secretService = secretService;
        }

        /// <summary>
        /// Is the user requesting the system proxy
        /// </summary>
        public WindowsProxyUsePolicy ProxyType => 
            _settings.Proxy.Url?.ToLower().Trim() switch
            {
                "[winhttp]" => WindowsProxyUsePolicy.UseWinHttpProxy,
                "[wininet]" => WindowsProxyUsePolicy.UseWinInetProxy,
                "[system]" => WindowsProxyUsePolicy.UseWinInetProxy,
                "" => WindowsProxyUsePolicy.DoNotUseProxy,
                null => WindowsProxyUsePolicy.DoNotUseProxy,
                _ => WindowsProxyUsePolicy.UseCustomProxy
        };

        public HttpMessageHandler GetHttpMessageHandler() => GetHttpMessageHandler(true);
        public HttpMessageHandler GetHttpMessageHandler(bool checkSsl = true)
        {
            var httpClientHandler = new LoggingHttpClientHandler(_log)
            {
                Proxy = GetWebProxy(),
                SslProtocols = SslProtocols,
            };
            if (!checkSsl)
            {
                httpClientHandler.ServerCertificateValidationCallback = (a, b, c, d) => true;
            }
            httpClientHandler.WindowsProxyUsePolicy = ProxyType;
            if (ProxyType == WindowsProxyUsePolicy.UseWinInetProxy)
            {
                httpClientHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            }
            return httpClientHandler;
        }

        /// <summary>
        /// Get prepared HttpClient with correct system proxy settings
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient(bool checkSsl = true)
        {
            var httpClientHandler = GetHttpMessageHandler(checkSsl);
            var httpClient = new HttpClient(httpClientHandler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"win-acme/{VersionService.SoftwareVersion} (+https://github.com/win-acme/win-acme)");
            return httpClient;
        }

        private class LoggingHttpClientHandler : WinHttpHandler
        {
            private readonly ILogService _log;

            public LoggingHttpClientHandler(ILogService log) => _log = log;

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _log.Debug("Send {method} request to {uri}", request.Method, request.RequestUri);
#if DEBUG
                if (request.Content != null)
                {
                    var content = await request.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        _log.Verbose("Request content: {content}", content);
                    }
                }
#endif
                var response = await base.SendAsync(request, cancellationToken);
                _log.Verbose("Request completed with status {s}", response.StatusCode);
#if DEBUG
                if (response.Content != null)
                {
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        _log.Verbose("Response content: {content}", content);
                    }
                }
#endif
                return response;
            }
        }


        /// <summary>
        /// Get proxy server to use for web requests
        /// </summary>
        /// <returns></returns>
        public IWebProxy? GetWebProxy()
        {
            if (_proxy == null)
            {
                var proxy = ProxyType switch {
                    WindowsProxyUsePolicy.UseCustomProxy => new WebProxy(_settings.Proxy.Url),
                    _ => null
                };
                if (proxy != null)
                {
                    if (!string.IsNullOrWhiteSpace(_settings.Proxy.Username))
                    {
                        var password = _secretService.EvaluateSecret(_settings.Proxy.Password);
                        proxy.Credentials = new NetworkCredential(_settings.Proxy.Username, password);
                    }
                    _log.Warning("Proxying via {proxy}:{port}", proxy.Address?.Host, proxy.Address?.Port);
                }
                _proxy = proxy;
            }
            return _proxy;
        }
    }
}
