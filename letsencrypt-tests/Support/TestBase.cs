using ACMESharp;
using ACMESharp.JOSE;
using ACMESharp.Messages;
using ACMESharp.PKI;
using ACMESharp.PKI.Providers;
using letsencrypt;
using letsencrypt.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NHttp;
using OpenSSL.X509;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace letsencrypt_tests.Support
{
    [TestClass]
    public class TestBase
    {
        protected Options MockOptions()
        {
            string currentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return new Options
            {
                ConfigPath = currentDirectory,
                Silent = true,
                Test = true
            };
        }

        protected bool StartFTPProxy = false;

        protected bool StartHTTPProxy = false;

        protected string HTTPProxyServer => $"localhost:{Settings.HTTPProxyPort}";

        public bool AllowInsecureSSLRequests = false;

        protected string FTPServerUrl { get { return $"ftp://127.0.0.1:{Settings.FTPProxyPort}"; } }

        [ClassInitialize]
        public static void FeatureTestClassInitialize(TestContext context)
        {
            Settings.Initialize(context);
        }

        [TestInitialize]
        public virtual void Initialize()
        {
            if (AllowInsecureSSLRequests)
            {
                DisableSSLCertValidation();
            }

            if (StartHTTPProxy)
            {
                MockHttpServer.Start(Settings.HTTPProxyPort, HttpServer_Request);
            }

            if (StartFTPProxy)
            {
                MockFtpServer.Start(Settings.FTPProxyPort);
            }
        }

        private void HttpServer_Request(object sender, HttpRequestEventArgs e)
        {
            MockHttpResponse mockresponse;
            string url = e.Request.RawUrl;
            Console.WriteLine(url);
            if (HasMockResponse(url, out mockresponse))
            {
                History.Add(url);
                if (e.Request.ContentLength > 0)
                {
                    byte[] bytes = new byte[e.Request.ContentLength];
                    try
                    {
                        e.Request.InputStream.Read(bytes, 0, e.Request.ContentLength);
                    }
                    catch { }
                    Requests[url] = 
                        mockresponse.RequestBody = Encoding.ASCII.GetString(bytes);
                }
                else
                {
                    Requests[url] = url;
                }
                mockresponse.GetResponse?.Invoke(e, mockresponse);
                e.Finish(mockresponse.ResponseBody, mockresponse.Headers ?? new MockHttpHeaders(), mockresponse.Encoding, mockresponse.StatusCode + " " + mockresponse.StatusDescription);
            }else
            {
                Console.WriteLine("No mock response found for {0}", url);
            }
        }

        /// <summary>
        /// The navigation history of the web browser
        /// </summary>
        public List<string> History = new List<string>();

        /// <summary>
        /// The history of requests that were mocked
        /// </summary>
        public Dictionary<string, string> Requests = new Dictionary<string, string>();

        private Dictionary<string, MockHttpResponse> mockResponses;

        /// <summary>
        /// Mocks the response for a specific <paramref name="url"/>
        /// </summary>
        /// <param name="url">The address to mock</param>
        /// <param name="response">The response to be returned when any url matching <paramref name="url"/> is requested</param>
        public void MockResponse(string url, MockHttpResponse response)
        {
            if (mockResponses == null)
            {
                mockResponses = new Dictionary<string, MockHttpResponse>();
            }
            mockResponses[url] = response;
        }

        protected string ProxyUrl(string url)
        {
            return ($"http://{HTTPProxyServer}{url}");
        }

        protected static string RandomString(int length = 8, string prefix = "")
        {
            string rndchars = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm1234567890";
            Random r = new Random();
            string s = prefix;
            while (s.Length < length)
            {
                s += rndchars.Substring(r.Next(0, rndchars.Length), 1);
            }
            return s;
        }

        protected static string RandomPhoneNumber()
        {
            return RandomNumber(2145550000, int.MaxValue).ToString();
        }

        protected static int RandomNumber(int min = 0, int max = 65535)
        {
            Random r = new Random((int)(DateTime.Now.Ticks));
            return r.Next(min, max);
        }

        /// <summary>
        /// Converts the object to an XML document string
        /// </summary>
        /// <param name="value">Any serializable object</param>
        /// <returns></returns>
        protected static string toXML(object value)
        {
            XmlSerializer serializer = new XmlSerializer(value.GetType());
            StringBuilder sb = new StringBuilder();
            using (StringWriter writer = new StringWriter(sb))
            {
                serializer.Serialize(writer, value);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts the object to a json-encoded string
        /// </summary>
        /// <param name="value">Any serializable object</param>
        /// <returns></returns>
        protected static string toJson(object value)
        {
            var jsonSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat
            };
            return JsonConvert.SerializeObject(value, Formatting.Indented, jsonSettings);
        }

        /// <summary>
        /// Reads the text of a file into a string
        /// </summary>
        /// <param name="filename">Any path to a file that exists</param>
        /// <returns></returns>
        protected static string fromFile(string filename)
        {
            return File.ReadAllText(Path.GetFullPath(filename));
        }

        /// <summary>
        /// Reads the contents of a file into a byte[]
        /// </summary>
        /// <param name="filename">Any path to a file that exists</param>
        /// <returns></returns>
        protected static byte[] fromFileBytes(string filename)
        {
            return File.ReadAllBytes(Path.GetFullPath(filename));
        }

        /// <summary>
        /// Safely converts <paramref name="value"/> to a string or <typeparamref name="null"/>
        /// </summary>
        /// <param name="value">Anything</param>
        /// <returns></returns>
        protected static string toString(object value)
        {
            try
            {
                return Convert.ToString(value);
            }
            catch
            {
            }
            return null;
        }

        protected static string removeLastSlash(string value)
        {
            if (value.EndsWith("/"))
            {
                return value.Substring(0, value.Length - 1);
            }
            return value;
        }

        /// <summary>
        /// Mocks the response for a specific <paramref name="url"/>
        /// </summary>
        /// <param name="url">The address to mock</param>
        /// <param name="response">The response to be returned when any url matching <paramref name="url"/> is requested</param>
        public void MockResponse(string url, string response)
        {
            MockResponse(url, new MockHttpResponse { ResponseBody = response });
        }

        private bool HasMockResponse(string url, out MockHttpResponse response)
        {
            response = null;
            if (mockResponses != null)
            {
                foreach (string key in mockResponses.Keys)
                {
                    if (Regex.IsMatch(url, key, RegexOptions.IgnoreCase))
                    {
                        response = mockResponses[key];
                        return true;
                    }
                }
            }
            return false;
        }

        protected AcmeClient MockAcmeClient(Options options)
        {
            MockResponse("/directory", new MockHttpResponse
            {
                Headers = new NameValueCollection
                {
                    [AcmeProtocol.HEADER_REPLAY_NONCE] = RandomString()
                },
                ResponseBody = toJson(new ObjectDictionary
                {
                    ["key-change"] = ProxyUrl("/acme/key-change"),
                    ["new-authz"] = ProxyUrl("/acme/new-authz"),
                    ["new-cert"] = ProxyUrl("/acme/new-cert"),
                    ["new-reg"] = ProxyUrl("/acme/new-reg"),
                    ["revoke-cert"] = ProxyUrl("/acme/revoke-cert")
                })
            });
            MockResponse("/acme/new-authz", new MockHttpResponse
            {
                Headers = new NameValueCollection
                {
                    [AcmeProtocol.HEADER_REPLAY_NONCE] = RandomString(),
                    [AcmeProtocol.HEADER_LOCATION] = ProxyUrl("/acme/testauthorizationlocation")
                },
                ResponseBody = toJson(new NewAuthzResponse
                {
                    Identifier = new IdentifierPart { Type = "host", Value = HTTPProxyServer },
                    Status = "pending",
                    Expires = DateTime.Now.AddDays(1),
                    Challenges = new[] {
                        new ChallengePart {
                            Type = AcmeProtocol.CHALLENGE_TYPE_HTTP,
                            Status = "pending",
                            Token = "test-token",
                            Uri = ProxyUrl("/acme/challenge")
                        }
                    },
                    Combinations = new[] { (new[] { 0 }).ToList() }
                })
            });
            MockResponse("/acme/challenge", new MockHttpResponse
            {
                Headers = new NameValueCollection
                {
                    [AcmeProtocol.HEADER_REPLAY_NONCE] = RandomString()
                },
                ResponseBody = "TUQCnSF5wPDRMVo2v3oFix1lSndX64Dj"
            });
            MockResponse("/acme/testauthorizationlocation", new MockHttpResponse
            {
                Headers = new NameValueCollection
                {
                    [AcmeProtocol.HEADER_REPLAY_NONCE] = RandomString()
                },
                ResponseBody = toJson(new AuthzStatusResponse
                {
                    Identifier = new IdentifierPart { Type = "host", Value = HTTPProxyServer },
                    Status = "valid",
                    Expires = DateTime.Now.AddDays(1),
                    Challenges = new[] {
                        new ChallengePart {
                            Type = AcmeProtocol.CHALLENGE_TYPE_HTTP,
                            Status = "pending",
                            Token = "test-token",
                            Uri = ProxyUrl("/acme/challenge")
                        }
                    },
                    Combinations = new[] { (new[] { 0 }).ToList() }
                })
            });
            MockResponse("/acme/new-cert", new MockHttpResponse
            {
                StatusCode = "201",
                StatusDescription = "Created",
                Headers = new NameValueCollection
                {
                    [AcmeProtocol.HEADER_REPLAY_NONCE] = RandomString(),
                    [AcmeProtocol.HEADER_LOCATION] = ProxyUrl("/acme/testcertlocation"),
                    ["Content-Type"] = "text/plain"
                },
                ResponseBody = Encoding.UTF8.GetString(fromFileBytes("test-cert.der"))
            });
            CertificateProvider.RegisterProvider<MockCertificateProvider>();
            var client = LetsEncrypt.CreateAcmeClient(options);
            return client;
        }

        private void DisableSSLCertValidation()
        {
            ServicePointManager.ServerCertificateValidationCallback =
              new RemoteCertificateValidationCallback(
                   delegate
                   { return true; }
               );
        }

        [TestCleanup]
        public virtual void Cleanup()
        {
            if (StartFTPProxy)
            {
                MockFtpServer.Stop();
            }
            if (StartHTTPProxy)
            {
                MockHttpServer.Stop();
            }
        }
    }
}
