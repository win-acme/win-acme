using NHttp;
using System.Net;

namespace letsencrypt_tests.Support
{
    public class MockHttpServer
    {
        private static HttpServer server;
        public static void Start(int listenPort, HttpRequestEventHandler handler)
        {
            if (server != null)
            {
                Stop();
            }
            server = new HttpServer();
            server.RequestReceived += (s, e) =>
            {
                handler(s, e);
            };
            server.EndPoint = new IPEndPoint(IPAddress.Loopback, listenPort);
            server.Start();
        }

        public static void Stop()
        {
            try
            {
                if (server != null)
                {
                    server.Stop();
                    server = null;
                }
            }
            catch { }
        }
    }
}
