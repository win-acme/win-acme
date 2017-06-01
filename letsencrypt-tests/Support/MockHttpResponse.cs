using NHttp;
using System.Collections.Specialized;
using System.Text;

namespace letsencrypt_tests.Support
{
    public class MockHttpResponse
    {
        public MockHttpResponse()
        {
            StatusCode = "200";
            StatusDescription = "OK";
            Encoding = Encoding.UTF8;
        }
        public Encoding Encoding { get; set; }
        public NameValueCollection Headers { get; set; }
        public string RequestBody { get; set; }
        public string ResponseBody { get; set; }
        public string StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public System.Func<HttpRequestEventArgs, MockHttpResponse, MockHttpResponse> GetResponse { get; set; }
    }
}