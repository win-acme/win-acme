using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace PKISharp.WACS.Services
{
    public interface IProxyService
    {
        SslProtocols SslProtocols { get; set; }
        WindowsProxyUsePolicy ProxyType { get; }
        HttpMessageHandler GetHttpMessageHandler();
        HttpClient GetHttpClient(bool checkSsl = true);
        IWebProxy? GetWebProxy();
    }
}
