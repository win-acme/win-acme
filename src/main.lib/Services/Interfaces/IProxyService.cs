using System.Net;
using System.Net.Http;
using System.Security.Authentication;

namespace PKISharp.WACS.Services
{
    public interface IProxyService
    {
        SslProtocols SslProtocols { get; set; }
        bool UseSystemProxy { get; }
        HttpClient GetHttpClient(bool checkSsl = true);
        IWebProxy? GetWebProxy();
    }
}
