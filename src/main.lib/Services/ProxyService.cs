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
                return _settings.Acme.Proxy.Equals("[System]", StringComparison.OrdinalIgnoreCase); ;
            }
        }

        /// <summary>
        /// Get prepared HttpClient with correct system proxy settings
        /// </summary>
        /// <returns></returns>
        public HttpClient GetHttpClient()
        {
            var httpClientHandler = new HttpClientHandler()
            {
                Proxy = GetWebProxy()
            };
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
                                string.IsNullOrEmpty(_settings.Acme.Proxy) ? 
                                    new WebProxy() : 
                                    new WebProxy(_settings.Acme.Proxy);
                if (proxy != null)
                {
                    var testUrl = new Uri("http://proxy.example.com");
                    var proxyUrl = proxy.GetProxy(testUrl);

                    if (!string.IsNullOrWhiteSpace(_settings.Acme.ProxyUsername))
                    {
                        proxy.Credentials = new NetworkCredential(
                            _settings.Acme.ProxyUsername,
                            _settings.Acme.ProxyPassword);
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
