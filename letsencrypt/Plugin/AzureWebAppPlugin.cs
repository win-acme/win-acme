using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Linq;

using letsencrypt.Support;

using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

using Serilog;

namespace letsencrypt
{
    [Serializable]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2229:ImplementSerializationConstructors")]
    public class ObjectDictionary : Dictionary<string, object> { }

    public class AzureWebAppPlugin : FTPPlugin
    {
        private Dictionary<string, string> config;

        private string access_token;
        public string hostName;
        private string subscriptionId;
        public JToken webApp;
        private string webAppName;

        public override string Name => R.AzureWebApp;

        public override bool RequiresElevated => false;

        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.Z;

        public override bool Validate(Options options)
        {
            config = GetConfig(options);
            try
            {
                RequireNotNull("tenant_id", config["tenant_id"]);
                RequireNotNull("client_id", config["client_id"]);
                RequireNotNull("client_secret", config["client_secret"]);
                var login = AzureRestApi.Login(config["tenant_id"], config["client_id"], config["client_secret"]);

                access_token = LetsEncrypt.GetString(login, "access_token");
                Log.Information(R.AzureADloginsuccessful);
            }
            catch (Exception e)
            {
                Log.Information(e, R.AzureADloginfailed);
                return false;
            }
            return !string.IsNullOrEmpty(access_token);
        }

        public override bool SelectOptions(Options options)
        {
            subscriptionId = LetsEncrypt.GetString(config, "subscription_id");
            if (string.IsNullOrEmpty(subscriptionId))
            {
                var subscriptions = AzureRestApi.GetSubscriptions(access_token);
                subscriptionId = LetsEncrypt.DisplayMenuOptions(options, subscriptions, R.EntertheAzuresubscriptionID, "displayName", "subscriptionId", false);
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

            webAppName = LetsEncrypt.GetString(config, "web_app_name");
            if (string.IsNullOrEmpty(webAppName))
            {
                webAppName = LetsEncrypt.DisplayMenuOptions(options, webApps, R.EntertheAzurewebappID, "name", "name", false);
                RequireNotNull("web_app_name", webAppName);
            }

            webApp = webApps.First(w => (string)w["name"] == webAppName);

            if (options.San)
            {
                Log.Information(R.SanCertificatesarenotsupportedbytheAzureWebAppPlugin);
            }

            hostName = LetsEncrypt.GetString(config, "host_name");
            if (string.IsNullOrEmpty(hostName))
            {
                JArray hostnames = GetHostNamesFromWebApp(webApp);
                hostName = LetsEncrypt.DisplayMenuOptions(options, hostnames, R.Selectthehostnamesforthecertificate, "name", "name", true);
                RequireNotNull("host_name", hostName);
            }
            return true;
        }

        public override void Install(Target target, Options options)
        {
            var publishingCredentials = AzureRestApi.GetPublishingCredentials(access_token, LetsEncrypt.GetString(webApp, "id"));

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

            var pfxFilename = Auto(target, options);
            if (!string.IsNullOrEmpty(pfxFilename))
            {
                ObjectDictionary installResult = AzureRestApi.InstallCertificate(options, access_token, subscriptionId, hostName, webApp, pfxFilename);
                if (installResult.ContainsKey("properties"))
                {
                    string thumbprint = LetsEncrypt.GetString((JToken)installResult["properties"], "thumbprint");
                    var setHostNameResult = AzureRestApi.SetCertificateHostName(access_token, subscriptionId, hostName, webApp, thumbprint);
                    if (!setHostNameResult.ContainsKey("properties"))
                    {
                        Log.Error(R.SSLBindingfailed);
                        Log.Error(JsonConvert.SerializeObject(setHostNameResult, Formatting.Indented));
                    }
                }
                else
                {
                    Log.Error(R.Certificateinstallationfailed);
                    Log.Error(JsonConvert.SerializeObject(installResult, Newtonsoft.Json.Formatting.Indented));
                }
            }
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override List<Target> GetTargets(Options options)
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
            Console.WriteLine(R.AzureWebAppMenuOption);
        }

        protected override Dictionary<string, string> GetConfig(Options options)
        {
            config = base.GetConfig(options);
            if (config.ContainsKey("tenant_id") &&
                config.ContainsKey("client_id") &&
                config.ContainsKey("client_secret"))
            {
                return config;
            }
            throw new FileNotFoundException(R.Configfileisincompleteordoesnotexist);
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

        //public override void BeforeAuthorize(Target target, string answerPath, string token)
        //{
        //    base.BeforeAuthorize(target, answerPath, token);
        //}

        //public override void CreateAuthorizationFile(string answerPath, string fileContents)
        //{
        //    base.CreateAuthorizationFile(answerPath, fileContents);
        //}

        //public override void DeleteAuthorization(Options options, string answerPath, string token, string webRootPath, string filePath)
        //{
        //    base.DeleteAuthorization(options, answerPath, token, webRootPath, filePath);
        //}
    }
}
