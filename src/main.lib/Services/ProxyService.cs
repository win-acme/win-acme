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
        public bool UseSystemProxy => string.Equals(_settings.Proxy.Url, "[System]", StringComparison.OrdinalIgnoreCase);

        public HttpClientHandler GetHttpClientHandler() => GetHttpClientHandler(true);
        public HttpClientHandler GetHttpClientHandler(bool checkSsl = true)
        {
            var httpClientHandler = new LoggingHttpClientHandler(_log)
            {
                Proxy = GetWebProxy(),
                SslProtocols = SslProtocols,
            };
            if (!checkSsl)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
            }
            if (UseSystemProxy)
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
            var httpClientHandler = GetHttpClientHandler(checkSsl);
            var httpClient = new HttpClient(httpClientHandler);
            httpClient.DefaultRequestHeaders.Add("User-Agent", $"win-acme/{VersionService.SoftwareVersion} (+https://github.com/win-acme/win-acme)");
            return httpClient;
        }

        private class LoggingHttpClientHandler : HttpClientHandler
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
                var proxy = UseSystemProxy ?
                                null :
                                string.IsNullOrEmpty(_settings.Proxy.Url) ?
                                    new WebProxy() :
                                    new WebProxy(_settings.Proxy.Url);
                if (proxy != null)
                {
                    if (!string.IsNullOrWhiteSpace(_settings.Proxy.Username))
                    {
                        var password = _secretService.EvaluateSecret(_settings.Proxy.Password);
                        proxy.Credentials = new NetworkCredential(_settings.Proxy.Username, password);
                    }
                    var testUrl = new Uri("http://proxy.example.com");
                    var proxyUrl = proxy.GetProxy(testUrl);
                    if (proxyUrl != null)
                    {

                        var useProxy = !string.Equals(testUrl.Host, proxyUrl.Host);
                        if (useProxy)
                        {
                            _log.Warning("Proxying via {proxy}:{port}", proxyUrl.Host, proxyUrl.Port);
                        }
                    }
                }
                _proxy = proxy;
            }
            return _proxy;
        }
    }
}
