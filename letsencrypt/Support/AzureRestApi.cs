using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Serilog;

namespace letsencrypt.Support
{
    public class AzureRestApi
    {
        public static string ApiRootUrl = "https://management.azure.com";

        public static string AuthRootUrl = "https://login.microsoftonline.com";

        static internal ObjectDictionary InstallCertificate(Options options, string access_token, string subscriptionId, string hostName, JToken webApp, string pfxFilename)
        {
            string resourceGroupName = (string)webApp["properties"]["resourceGroup"];
            string[] hostNames = hostName.Split(", ;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string name = (string)webApp["properties"]["name"];
            string url = $"{ApiRootUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/certificates/{name}?api-version=2016-03-01";
            string password = options.PFXPassword;
            string pfxBlob = Convert.ToBase64String(new X509Certificate2(pfxFilename, password, X509KeyStorageFlags.Exportable).Export(X509ContentType.Pfx, password));
            var body = new ObjectDictionary
            {
                ["kind"] = "pfx",
                ["location"] = webApp["location"],
                ["name"] = name,
                ["type"] = "Microsoft.Web/certificates",
                ["properties"] = new ObjectDictionary
                {
                    ["hostNames"] = hostNames,
                    ["pfxBlob"] = pfxBlob,
                    ["password"] = password,
                    ["serverFarmId"] = webApp["properties"]["serverFarmId"]
                }
            };
            var headers = TokenBearerHeaders(access_token);
            headers.Add("Content-Type", "application/json");
            string result;
            try
            {
                result = Put(url, headers, body);
            }
            catch (WebException e)
            {
                Log.Error(e, R.Installationofthecertificatefailed);
                result = ReadErrorResponse(e);
                if (result == null)
                {
                    throw;
                }
            }
            return JsonConvert.DeserializeObject<ObjectDictionary>(result);
        }

        private static string ReadErrorResponse(WebException e)
        {
            string result;
            using (Stream s = e.Response.GetResponseStream())
            {
                long len = s.Length;
                byte[] buff = new byte[len];
                s.Read(buff, 0, (int)len);
                result = Encoding.UTF8.GetString(buff);
            }

            return result;
        }

        static internal ObjectDictionary SetCertificateHostName(string access_token, string subscriptionId, string hostName, JToken webApp, string thumbprint)
        {
            string name = (string)webApp["name"];
            string resourceGroupName = (string)webApp["properties"]["resourceGroup"];
            string webAppId = (string)webApp["id"];
            string url = $"{ApiRootUrl}{webAppId}?api-version=2016-08-01";
            string[] hostNames = hostName.Split(", ;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            List<ObjectDictionary> hostNameSslStates = new List<ObjectDictionary>(hostNames.Length);
            foreach (string host in hostNames)
            {
                hostNameSslStates.Add(new ObjectDictionary
                {
                    ["name"] = host,
                    ["sslState"] = "SniEnabled",
                    ["thumbprint"] = thumbprint,
                    ["toUpdate"] = true,
                    ["ipBasedSslState"] = "NotConfigured",
                    ["hostType"] = "Standard"
                });
            }
            var data = new ObjectDictionary
            {
                ["name"] = name,
                ["type"] = webApp["type"],
                ["location"] = webApp["location"],
                ["properties"] = new ObjectDictionary
                {
                    ["hostNameSslStates"] = hostNameSslStates
                }
            };
            var headers = TokenBearerHeaders(access_token);
            headers.Add("Content-Type", "application/json");
            string result;
            try
            {
                result = Put(url, headers, data);
            }
            catch (WebException e)
            {
                Log.Error(e, R.Associationofthecertificatefailed);
                result = ReadErrorResponse(e);
                if (result == null)
                {
                    throw;
                }
            }
            return JsonConvert.DeserializeObject<ObjectDictionary>(result);
        }

        static internal XmlDocument GetPublishingCredentials(string access_token, string webAppId)
        {
            string url = $"{ApiRootUrl}{webAppId}/publishxml?api-version=2016-08-01";
            var xml = Post(url, TokenBearerHeaders(access_token));
            var xdoc = new XmlDocument();
            xdoc.LoadXml(xml);
            return xdoc;
        }

        static internal JArray GetWebApps(string access_token, string subscriptionId, string resourceGroupName)
        {
            string url = $"{ApiRootUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites?api-version=2016-08-01&includeSlots=true";
            var results = Get<ObjectDictionary>(url, TokenBearerHeaders(access_token));
            return (JArray)results["value"];
        }

        static internal JArray GetResourceGroups(string access_token, string subscriptionId)
        {
            var results = Get<ObjectDictionary>($"{ApiRootUrl}/subscriptions/{subscriptionId}/resourcegroups?api-version=2016-06-01", TokenBearerHeaders(access_token));
            return (JArray)results["value"];
        }

        static internal JArray GetSubscriptions(string access_token)
        {
            var results = Get<ObjectDictionary>($"{ApiRootUrl}/subscriptions?api-version=2016-06-01", TokenBearerHeaders(access_token));
            return (JArray)results["value"];
        }

        static internal ObjectDictionary Login(string tenantId, string client_id, string client_secret)
        {
            var data = new ObjectDictionary
            {
                ["grant_type"] = "client_credentials",
                ["resource"] = ApiRootUrl + "/",
                ["client_id"] = client_id,
                ["client_secret"] = client_secret
            };

            return Post<ObjectDictionary>($"{AuthRootUrl}/{tenantId}/oauth2/token", null, data);
        }

        #region private functions

        private static Dictionary<string, string> TokenBearerHeaders(string access_token)
        {
            return new Dictionary<string, string> { ["Authorization"] = $"Bearer {access_token}" };
        }

        private static T Get<T>(string Url, Dictionary<string, string> headers = null)
        {
            string result;
            using (WebClient client = new WebClient())
            {
                if (headers != null)
                {
                    foreach (var kv in headers)
                    {
                        client.Headers.Set(kv.Key, kv.Value);
                    }
                }
                result = client.DownloadString(Url);
            }
            return JsonConvert.DeserializeObject<T>(result);
        }

        private static T Post<T>(string Url, Dictionary<string, string> headers = null, ObjectDictionary data = null)
        {
            string decoded = Post(Url, headers, data);
            return JsonConvert.DeserializeObject<T>(decoded);
        }

        private static string Post(string Url, Dictionary<string, string> headers = null, ObjectDictionary data = null)
        {
            return Upload("POST", Url, headers, data);
        }

        private static string Put(string Url, Dictionary<string, string> headers = null, ObjectDictionary data = null)
        {
            return Upload("PUT", Url, headers, data);
        }

        private static string Upload(string method, string Url, Dictionary<string, string> headers = null, ObjectDictionary data = null)
        {
            byte[] result;
            using (WebClient client = new WebClient())
            {
                if (headers != null)
                {
                    foreach (var kv in headers)
                    {
                        client.Headers.Set(kv.Key, kv.Value);
                    }
                }
                if (!client.Headers.AllKeys.Contains("Content-Type"))
                {
                    client.Headers.Set(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");
                }
                bool isJson = client.Headers.Get("Content-Type").EndsWith("json");
                byte[] upload = isJson
                    ? Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data))
                    : Encoding.UTF8.GetBytes(Urlencode(data));
                result = client.UploadData(Url, method, upload);
            }
            return Encoding.UTF8.GetString(result);
        }

        //private static string PostJson(string Url, Dictionary<string, string> headers = null, ObjectDictionary data = null)
        //{
        //    byte[] result;
        //    using (WebClient client = new WebClient())
        //    {
        //        if (headers != null)
        //        {
        //            foreach (var kv in headers)
        //            {
        //                client.Headers.Set(kv.Key, kv.Value);
        //            }
        //        }
        //        client.Headers.Set(HttpRequestHeader.ContentType, "application/json");
        //        byte[] upload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
        //        result = client.UploadData(Url, upload);
        //    }
        //    return Encoding.UTF8.GetString(result);
        //}

        private static string Urlencode(ObjectDictionary data)
        {
            List<string> list = new List<string>();
            if (data != null)
            {
                foreach (var k in data)
                {
                    string encoded = Uri.EscapeDataString(Convert.ToString(k.Value));
                    list.Add($"{k.Key}={encoded}");
                }
            }
            return string.Join("&", list);
        }

        #endregion
    }
}
