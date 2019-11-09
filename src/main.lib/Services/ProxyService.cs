using System;
using System.Net;
using System.Net.Http;

namespace PKISharp.WACS.Services
{
    internal class ProxyService
    {
        private readonly ILogService _log;
        private IWebProxy _proxy;
        private readonly ISettingsService _settings;

        public ProxyService(ILogService log, ISettingsService settings)
        {
            _log = log;
            _settings = settings;
        }

        /// <summary>
        /// Is the user requesting the system proxy
        /// </summary>
        public bool UseSystemProxy
        {
            get
            {
                return _settings.Proxy.Url.Equals("[System]", StringComparison.OrdinalIgnoreCase); ;
            }
        }

        /// <summary>
        /// Get prepared HttpClient with correct system proxy settings
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient(bool checkSsl = true)
        {
            var httpClientHandler = new HttpClientHandler()
            {
                Proxy = GetWebProxy()
            };
            if (!checkSsl)
            {
                httpClientHandler.ServerCertificateCustomValidationCallback = (a, b, c, d) => true;
            }
            if (UseSystemProxy)
            {
                httpClientHandler.DefaultProxyCredentials = CredentialCache.DefaultCredentials;
            }
            return new HttpClient(httpClientHandler);
        }

        /// <summary>
        /// Get proxy server to use for web requests
        /// </summary>
        /// <returns></returns>
        public IWebProxy GetWebProxy()
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
                    var testUrl = new Uri("http://proxy.example.com");
                    var proxyUrl = proxy.GetProxy(testUrl);

                    if (!string.IsNullOrWhiteSpace(_settings.Proxy.Username))
                    {
                        proxy.Credentials = new NetworkCredential(
                            _settings.Proxy.Username,
                            _settings.Proxy.Password);
                    }

                    var useProxy = !string.Equals(testUrl.Host, proxyUrl.Host);
                    if (useProxy)
                    {
                        _log.Warning("Proxying via {proxy}:{port}", proxyUrl.Host, proxyUrl.Port);
                    }
                }
                _proxy = proxy;
            }
            return _proxy;
        }

    }
}
