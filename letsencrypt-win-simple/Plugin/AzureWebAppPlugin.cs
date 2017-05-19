using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net;
using System.Linq;

using LetsEncrypt.ACME.Simple.Support;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Serilog;
using System.Globalization;

namespace LetsEncrypt.ACME.Simple
{
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors")]
    internal class ObjectDictionary : Dictionary<string, object> { }

    internal class AzureWebAppPlugin : Plugin
    {
        private Dictionary<string, string> config;

        private string access_token;
        private string hostName;
        private string subscriptionId;
        private JToken webApp;
        private string webAppName;

        public override string Name => "Azure Web App";

        public override bool RequiresElevated => false;

        public NetworkCredential FtpCredentials { get; set; }

        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.Z;

        public override bool Validate()
        {
            config = GetConfig();
            try
            {
                RequireNotNull("tenant_id", config["tenant_id"]);
                RequireNotNull("client_id", config["client_id"]);
                RequireNotNull("client_secret", config["client_secret"]);
                var login = AzureRestApi.Login(config["tenant_id"], config["client_id"], config["client_secret"]);

                access_token = getString(login, "access_token");
                Log.Information("Azure AD login successful");
            }
            catch (Exception e)
            {
                Log.Information(e, "Azure AD login failed");
                return false;
            }
            return !string.IsNullOrEmpty(access_token);
        }

        public override bool SelectOptions(Options options)
        {
            subscriptionId = getString(config, "subscription_id");
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

            webAppName = getString(config, "web_app_name");
            if (string.IsNullOrEmpty(webAppName))
            {
                webAppName = DisplayMenuOptions(webApps, "Enter the Azure web app ID", "name", "name", false);
                RequireNotNull("web_app_name", webAppName);
            }

            webApp = webApps.First(w => (string)w["name"] == webAppName);

            if (options.San)
            {
                Console.WriteLine("San Certificates are not supported for Azure Web App Plugin.");
            }

            hostName = getString(config, "host_name");
            if (string.IsNullOrEmpty(hostName))
            {
                JArray hostnames = GetHostNamesFromWebApp(webApp);
                hostName = DisplayMenuOptions(hostnames, "Select the host names for the certificate\n " +
                    "You can enter multiple IDs comma-separated e.g. 1,2,3", "name", "name", true);
                RequireNotNull("host_name", hostName);
            }
            return true;
        }

        public override void Install(Target target, Options options)
        {
            var publishingCredentials = AzureRestApi.GetPublishingCredentials(access_token, getString(webApp, "id"));

            var ftp = publishingCredentials.SelectSingleNode("//publishProfile[@publishMethod='FTP']");
            var ftpUsername = ftp.Attributes["userName"].Value;
            var ftpPassword = ftp.Attributes["userPWD"].Value;
            var publishUrl = ftp.Attributes["publishUrl"].Value;

            FtpCredentials = new NetworkCredential(ftpUsername, ftpPassword);
            string[] hosts = hostName.Split(',');
            string primary = hosts.First();
            target.Host = primary;
            target.WebRootPath = publishUrl;
            target.AlternativeNames = new List<string>(hosts.Except(new string[] { primary }));

            var auth = Authorize(target, options);
            if (auth.Status == "valid")
            {
                var pfxFilename = GetCertificate(target, client, options);
                ObjectDictionary installResult = AzureRestApi.InstallCertificate(access_token, subscriptionId, hostName, webApp, pfxFilename);
                if (installResult.ContainsKey("properties"))
                {
                    string thumbprint = getString((JToken)installResult["properties"], "thumbprint");
                    var setHostNameResult = AzureRestApi.SetCertificateHostName(access_token, subscriptionId, hostName, webApp, thumbprint);
                    if (!setHostNameResult.ContainsKey("properties"))
                    {
                        Log.Error("SSL Binding failed");
                        Log.Error(JsonConvert.SerializeObject(setHostNameResult, Formatting.Indented));
                    }
                }
                else
                {
                    Log.Error("Certificate installation failed");
                    Log.Error(JsonConvert.SerializeObject(installResult, Newtonsoft.Json.Formatting.Indented));
                }
            }
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();
            result.Add(new Target
            {
                PluginName = Name
            });
            return result;
        }

        public override void PrintMenu()
        {
            Console.WriteLine(" Z: Install a certificate for an Azure Web App.");
        }

        private static void RequireNotNull(string field, string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentNullException(field);
            }
        }

        private static Dictionary<string, string> GetConfig()
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
            throw new FileNotFoundException("Config file: AzureWebApp.json is incomplete or does not exist.", configFile);
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

        private static JArray GetHostNamesFromWebApp(JToken webApp)
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
            while (values.Count == 0 && !LetsEncrypt.Options.Silent)
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
}
