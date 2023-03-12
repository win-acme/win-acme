using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using static ACMESharp.Protocol.AcmeProtocolClient;

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

            /// <summary>
            /// Synchronous request (note that this cannot call SendAsync,
            /// see issue #2311)
            /// </summary>
            /// <param name="request"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            protected override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken) 
            {
                PreSend(request, cancellationToken).RunSynchronously();
                var response = base.Send(request, cancellationToken);
                PostSend(response, cancellationToken).RunSynchronously();
                return response;
            }

            /// <summary>
            /// Asynchronous request
            /// </summary>
            /// <param name="request"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                await PreSend(request, cancellationToken);
                var response = await base.SendAsync(request, cancellationToken);
                await PostSend(response, cancellationToken);
                return response;
            }
        
            /// <summary>
            /// Common pre-send functionality
            /// </summary>
            /// <param name="request"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            private async Task PreSend(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                _log.Debug("[HTTP] Send {method} to {uri}", request.Method, request.RequestUri);
                if (request.Content != null)
                {
                    var content = await request.Content.ReadAsStringAsync(cancellationToken);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        _log.Verbose("[HTTP] Request content: {content}", content);
                    }
                }
            }

            /// <summary>
            /// Common post-send functionality
            /// </summary>
            /// <param name="response"></param>
            /// <param name="cancellationToken"></param>
            /// <returns></returns>
            private async Task PostSend(HttpResponseMessage response, CancellationToken cancellationToken)
            {
                _log.Verbose("[HTTP] Request completed with status {s}", response.StatusCode);
                if (response.Content != null && response.Content.Headers.ContentLength > 0)
                {
                    var printableTypes = new[] {
                        "text/json",
                        "application/json",
                        "application/problem+json"
                    };
                    if (printableTypes.Contains(response.Content.Headers.ContentType?.MediaType))
                    {
                        var content = await response.Content.ReadAsStringAsync(cancellationToken);
                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            _log.Verbose("[HTTP] Response content: {content}", content);
                        }
                    }
                    else
                    {
                        _log.Verbose("[HTTP] Response of type {type} ({bytes} bytes)", response.Content.Headers.ContentType?.MediaType, response.Content.Headers.ContentLength);
                    }
                }
                else
                {
                    _log.Verbose("[HTTP] Empty response");
                }
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
