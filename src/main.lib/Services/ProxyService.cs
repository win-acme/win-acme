using System;
using System.Net;

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
        /// Get proxy server to use for web requests
        /// </summary>
        /// <returns></returns>
        public IWebProxy GetWebProxy()
        {
            if (_proxy == null)
            {
                var system = "[System]";
                var useSystem = _settings.Proxy.Equals(system, StringComparison.OrdinalIgnoreCase);
                var proxy = string.IsNullOrWhiteSpace(_settings.Proxy)
                    ? new WebProxy()
                    : useSystem
                        ? WebRequest.GetSystemWebProxy()
                        : new WebProxy(_settings.Proxy);

                if (proxy != null)
                {
                    var testUrl = new Uri("http://proxy.example.com");
                    var proxyUrl = proxy.GetProxy(testUrl);

                    if (!string.IsNullOrWhiteSpace(_settings.ProxyUsername))
                    {
                        proxy.Credentials = new NetworkCredential(
                            _settings.ProxyUsername,
                            _settings.ProxyPassword);
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
