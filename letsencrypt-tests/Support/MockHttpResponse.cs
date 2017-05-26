using System.Collections.Generic;
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
        public string ResponseBody { get; set; }
        public string StatusCode { get; set; }
        public string StatusDescription { get; set; }
        public NameValueCollection Headers { get; set; }
        public Encoding Encoding { get; set; }
    }
}