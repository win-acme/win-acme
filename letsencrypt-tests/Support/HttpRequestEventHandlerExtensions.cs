using NHttp;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace letsencrypt_tests.Support
{
    public static class HttpRequestEventHandlerExtensions
    {
        public static void Finish(this HttpRequestEventArgs args, string responseBody, NameValueCollection headers, Encoding encoding, string status = "200 OK")
        {
            args.Response.Status = status;
            args.Response.HeadersEncoding = encoding;
            args.Response.Headers.Add(headers);
            if (args.Response.Headers["Content-Length"] == null)
            {
                args.Response.Headers.Add("Content-Length", encoding.GetByteCount(responseBody).ToString());
            }
            using (var writer = new StreamWriter(args.Response.OutputStream))
            {
                writer.Write(responseBody);
            }
        }
    }
}
