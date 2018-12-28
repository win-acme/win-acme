using System;
using System.Net;

namespace PKISharp.WACS.Services
{
    internal class ProxyService
    {
        private ILogService _log;
        private IWebProxy _proxy;

        public ProxyService(ILogService log)
        {
            _log = log;
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
                var useSystem = Properties.Settings.Default.Proxy.Equals(system, StringComparison.OrdinalIgnoreCase);
                var proxy = string.IsNullOrWhiteSpace(Properties.Settings.Default.Proxy)
                    ? null
                    : useSystem
                        ? WebRequest.GetSystemWebProxy()
                        : new WebProxy(Properties.Settings.Default.Proxy);

                if (proxy != null)
                {
                    var testUrl = new Uri("http://proxy.example.com");
                    var proxyUrl = proxy.GetProxy(testUrl);

                    if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.ProxyUsername))
                    {
                        proxy.Credentials = new NetworkCredential(Properties.Settings.Default.ProxyUsername, Properties.Settings.Default.ProxyPassword);
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
