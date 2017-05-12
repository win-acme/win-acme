using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using System.Net;
using Newtonsoft.Json;
using System.Text;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.Xml;

namespace LetsEncrypt.ACME.Simple
{
    public class ObjectDictionary : Dictionary<string, object> { }

    public class AzureWebAppPlugin : Plugin
    {
        public override string Name => "Azure Web App";

        public NetworkCredential FtpCredentials { get; set; }

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();

            return result;
        }

        public override List<Target> GetSites()
        {
            var result = new List<Target>();

            return result;
        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            if (!string.IsNullOrWhiteSpace(Program.Options.Script) &&
                !string.IsNullOrWhiteSpace(Program.Options.ScriptParameters))
            {
                var parameters = string.Format(Program.Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword,
                    pfxFilename, store.Name, certificate.FriendlyName, certificate.Thumbprint);
                Log.Information("Running {Script} with {parameters}", Program.Options.Script, parameters);
                Process.Start(Program.Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Program.Options.Script))
            {
                Log.Information("Running {Script}", Program.Options.Script);
                Process.Start(Program.Options.Script);
            }
            else
            {
                Console.WriteLine(" WARNING: Unable to configure server software.");
            }
        }

        public override void Install(Target target)
        {
            // This method with just the Target parameter is currently only used by Centralized SSL
            if (!string.IsNullOrWhiteSpace(Program.Options.Script) &&
                !string.IsNullOrWhiteSpace(Program.Options.ScriptParameters))
            {
                var parameters = string.Format(Program.Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword, Program.Options.CentralSslStore);
                Log.Information("Running {Script} with {parameters}", Program.Options.Script, parameters);
                Process.Start(Program.Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Program.Options.Script))
            {
                Log.Information("Running {Script}", Program.Options.Script);
                Process.Start(Program.Options.Script);
            }
            else
            {
                Console.WriteLine(" WARNING: Unable to configure server software.");
            }
        }

        public override void Renew(Target target)
        {
            HandleMenuResponse("", new List<Target>(new[] { target }));
        }

        public override void PrintMenu()
        {
            Console.WriteLine(" Z: Install a certificate for an Azure Web App.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "" || response == "z")
            {
                Dictionary<string, string> config = GetConfig();
                string access_token;
                RequireNotNull("tenant_id", config["tenant_id"]);
                RequireNotNull("client_id", config["client_id"]);
                RequireNotNull("client_secret", config["client_secret"]);
                try
                {
                    var login = AzureRestApi.Login(config["tenant_id"], config["client_id"], config["client_secret"]);

                    access_token = getString(login, "access_token");
                }
                catch (Exception e)
                {
                    Log.Information(e, "Azure AD login failed");
                    return;
                }

                Log.Information("Azure AD login successful");

                string subscriptionId = getString(config, "subscription_id");
                if (string.IsNullOrEmpty(subscriptionId))
                {
                    var subscriptions = AzureRestApi.GetSubscriptions(access_token);
                    subscriptionId = DisplayMenuOptions(subscriptions, "Enter the Azure subscription ID", "displayName", "subscriptionId", false);
                    RequireNotNull("subscription_id", subscriptionId);
                }

                JArray webApps = new JArray();
                var resourceGroups = AzureRestApi.GetResourceGroups(access_token, subscriptionId);
                foreach (var resourceGroup in resourceGroups)
                {
                    var apps = AzureRestApi.GetWebApps(access_token, subscriptionId, (string)resourceGroup["name"]);
                    foreach (var app in apps)
                    {
                        webApps.Add(app);
                    }
                }
                
                string webAppName = getString(config, "web_app_name");
                if (string.IsNullOrEmpty(webAppName))
                {
                    webAppName = DisplayMenuOptions(webApps, "Enter the Azure web app ID", "name", "name", false);
                    RequireNotNull("web_app_name", webAppName);
                }

                var webApp = webApps.First(w => (string)w["name"] == webAppName);

                string hostName = getString(config, "host_name");
                if (string.IsNullOrEmpty(hostName))
                {
                    JArray hostnames = GetHostNamesFromWebApp(webApp);
                    hostName = DisplayMenuOptions(hostnames, "Select the host names for the certificate\n " +
                        "You can enter multiple IDs comma-separated e.g. 1,2,3", "name", "name", true);
                    RequireNotNull("host_name", hostName);
                }

                if (Program.Options.San)
                {
                    Console.WriteLine("San Certificates are not supported for Azure Web App Plugin.");
                }

                var publishingCredentials = AzureRestApi.GetPublishingCredentials(access_token, getString(webApp, "id"));

                var ftp = publishingCredentials.SelectSingleNode("//publishProfile[@publishMethod='FTP']");
                var ftpUsername = ftp.Attributes["userName"].Value;
                var ftpPassword = ftp.Attributes["userPWD"].Value;
                var publishUrl = ftp.Attributes["publishUrl"].Value;

                this.FtpCredentials = new NetworkCredential(ftpUsername, ftpPassword);
                string[] hosts = hostName.Split(',');
                string primary = hosts.First();
                var target = new Target()
                {
                    Host = primary,
                    WebRootPath = publishUrl,
                    PluginName = Name,
                    AlternativeNames = new List<string>(hosts.Except(new string[] { primary }))
                };

                var auth = Program.Authorize(target, client);
                if (auth.Status == "valid")
                {
                    var pfxFilename = Program.GetCertificate(target, client);
                    ObjectDictionary installResult = AzureRestApi.InstallCertificate(access_token, subscriptionId, hostName, webApp, pfxFilename);
                    if (installResult.ContainsKey("properties"))
                    {
                        string thumbprint = getString((JToken)installResult["properties"], "thumbprint");
                        var setHostNameResult = AzureRestApi.SetCertificateHostName(access_token, subscriptionId, hostName, webApp, thumbprint);
                        if (!setHostNameResult.ContainsKey("properties"))
                        {
                            Log.Error(JsonConvert.SerializeObject(setHostNameResult, Newtonsoft.Json.Formatting.Indented));
                        }
                        else
                        {
                            Log.Error("SSL Binding failed");
                            Log.Error(JsonConvert.SerializeObject(setHostNameResult, Newtonsoft.Json.Formatting.Indented));
                        }
                    }
                    else
                    {
                        Log.Error("Certificate installation failed");
                        Log.Error(JsonConvert.SerializeObject(installResult, Newtonsoft.Json.Formatting.Indented));
                    }
                }
            }
        }

        private void RequireNotNull(string field, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(field);
            }
        }

        private Dictionary<string, string> GetConfig()
        {
            string configFile = Path.GetFullPath("AzureWebApp.json");
            if (File.Exists(configFile))
            {
                string text = File.ReadAllText(configFile);
                var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(text);
                if (config.ContainsKey("tenant_id") &&
                    config.ContainsKey("client_id") &&
                    config.ContainsKey("client_secret"))
                {
                    return config;
                }
            }
            throw new Exception("Config file: AzureWebApp.json is incomplete or does not exist.");
        }

        private static string getString(Dictionary<string, string> dict, string key, string defaultValue = null)
        {
            if (dict.ContainsKey(key))
            {
                return dict[key];
            }
            return defaultValue;
        }

        private static string getString(ObjectDictionary dict, string key, string defaultValue = null)
        {
            if (dict.ContainsKey(key))
            {
                return (string)dict[key];
            }
            return defaultValue;
        }

        private static string getString(JToken obj, string key, string defaultValue = null)
        {
            try
            {
                return (string)obj[key];
            }
            catch { }
            return defaultValue;
        }

        private JArray GetHostNamesFromWebApp(JToken webApp)
        {
            JArray result = new JArray();
            foreach (string hostname in webApp["properties"]["hostNames"])
            {
                if (!hostname.EndsWith("azurewebsites.net"))
                {
                    result.Add(new JObject { ["name"] = hostname });
                }
            }
            return result;
        }

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            FTPPlugin ftp = new FTPPlugin();
            ftp.FtpCredentials = FtpCredentials;
            ftp.BeforeAuthorize(target, answerPath, token);
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            FTPPlugin ftp = new FTPPlugin();
            ftp.FtpCredentials = FtpCredentials;
            ftp.CreateAuthorizationFile(answerPath, fileContents);
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            FTPPlugin ftp = new FTPPlugin();
            ftp.FtpCredentials = FtpCredentials;
            ftp.DeleteAuthorization(answerPath, token, webRootPath, filePath);
        }

        private static string DisplayMenuOptions(JArray options, string message, string displayKey, string valueKey, bool multiSelection)
        {
            if (options.Count == 1)
            {
                return getString(options.First, valueKey);
            }
            Console.WriteLine();
            int i = 1;
            int width = options.Count.ToString().Length;
            foreach (var sub in options)
            {
                string index = Pad(i, width);
                Console.WriteLine($"{index}: {sub[displayKey]}");
                i++;
            }
            string value = "";
            List<string> values = new List<string>();
            while (values.Count == 0 && !Program.Options.Silent)
            {
                Console.Write("\n" + message + ": ");
                value = Console.ReadLine();
                string[] entries = value.Split(", ;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                foreach (string v in entries)
                {
                    if (int.TryParse(v, out i) && 0 < i && i <= options.Count)
                    {
                        var option = options[i - 1];
                        values.Add(getString(option, valueKey));
                        if (!multiSelection)
                        {
                            break;
                        }
                    }
                    else
                    {
                        value = "";
                    }
                }
            }
            return string.Join(",", values);
        }

        private static string Pad(int number, int width)
        {
            string result = number.ToString();
            while (result.Length < width) { result = " " + result; }
            return result;
        }
    }

    internal class AzureRestApi
    {
        static string apiRootUrl = "https://management.azure.com";

        static internal ObjectDictionary InstallCertificate(string access_token, string subscriptionId, string hostName, JToken webApp, string pfxFilename)
        {
            string resourceGroupName = (string)webApp["properties"]["resourceGroup"];
            string[] hostNames = hostName.Split(", ;".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            string name = (string)webApp["properties"]["name"];
            string url = $"{apiRootUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/certificates/{name}?api-version=2016-03-01";
            string password = Properties.Settings.Default.PFXPassword;
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
                Log.Error(e, "Installation of the certificate failed");
                result = ReadErrorResponse(e);
                if (result == null)
                {
                    throw e;
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
            string url = $"{apiRootUrl}{webAppId}?api-version=2016-08-01";
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
                Log.Error(e, "Association of the certificate failed");
                result = ReadErrorResponse(e);
                if (result == null)
                {
                    throw e;
                }
            }
            return JsonConvert.DeserializeObject<ObjectDictionary>(result);
        }

        static internal XmlDocument GetPublishingCredentials(string access_token, string webAppId)
        {
            string url = $"{apiRootUrl}{webAppId}/publishxml?api-version=2016-08-01";
            var xml = Post(url, TokenBearerHeaders(access_token));
            var xdoc = new XmlDocument();
            xdoc.LoadXml(xml);
            return xdoc;
        }

        static internal JArray GetWebApps(string access_token, string subscriptionId, string resourceGroupName)
        {
            string url = $"{apiRootUrl}/subscriptions/{subscriptionId}/resourceGroups/{resourceGroupName}/providers/Microsoft.Web/sites?api-version=2016-08-01&includeSlots=true";
            var results = Get<ObjectDictionary>(url, TokenBearerHeaders(access_token));
            return (JArray)results["value"];
        }

        static internal JArray GetResourceGroups(string access_token, string subscriptionId)
        {
            var results = Get<ObjectDictionary>($"{apiRootUrl}/subscriptions/{subscriptionId}/resourcegroups?api-version=2016-06-01", TokenBearerHeaders(access_token));
            return (JArray)results["value"];
        }

        static internal JArray GetSubscriptions(string access_token)
        {
            var results = Get<ObjectDictionary>($"{apiRootUrl}/subscriptions?api-version=2016-06-01", TokenBearerHeaders(access_token));
            return (JArray)results["value"];
        }

        static internal ObjectDictionary Login(string tenantId, string client_id, string client_secret)
        {
            var data = new ObjectDictionary
            {
                ["grant_type"] = "client_credentials",
                ["resource"] = apiRootUrl + "/",
                ["client_id"] = client_id,
                ["client_secret"] = client_secret
            };

            return Post<ObjectDictionary>($"https://login.microsoftonline.com/{tenantId}/oauth2/token", null, data);
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
