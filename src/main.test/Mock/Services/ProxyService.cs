using PKISharp.WACS.Services;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace PKISharp.WACS.UnitTests.Mock.Services
{
    internal class ProxyService : IProxyService
    {
        public SslProtocols SslProtocols { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
        public WindowsProxyUsePolicy ProxyType => throw new System.NotImplementedException();
        public HttpClient GetHttpClient(bool checkSsl = true) => throw new System.NotImplementedException();
        public HttpMessageHandler GetHttpMessageHandler() => throw new System.NotImplementedException();
        public IWebProxy? GetWebProxy() => throw new System.NotImplementedException();
    }
}