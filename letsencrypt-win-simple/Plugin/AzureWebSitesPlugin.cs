using LetsEncrypt.ACME.Simple;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.WebSites;
using Microsoft.Rest.Azure.Authentication;
using Microsoft.Rest;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net;
using System.IO;
using System.Xml;
using Microsoft.Azure.Management.WebSites.Models;
using Microsoft.Azure.Management.Resources;

namespace LetsEncrypt.ACME.SimplePlugin
{
    public class AzureWebSitesPlugin : Plugin
    {
        const string webConfig = @"<?xml version = ""1.0"" encoding=""UTF-8""?>
 <configuration>
     <system.webServer>
         <staticContent>
             <mimeMap fileExtension = ""."" mimeType=""text/json"" />
         </staticContent>
     </system.webServer>
 </configuration>";

        public override string Name => "Azure Web Site";

        private string _resourceGroupname;
        private WebSiteManagementClient _webSiteManagementClient;
        private string _siteName;

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();

            return result;
        }

        public override void BeforeAuthorize(Target target, string answerPath)
        {
            var client = _webSiteManagementClient;

            //PublishingProfile
            using (var xmlStream = client.Sites.ListSitePublishingProfileXml(_resourceGroupname, _siteName, new CsmPublishingProfileOptions()))
            {

                var publishData = new PublishData(xmlStream);
                var ftpProfile = publishData.PublishProfiles.First(s => s.PublishMethod == "FTP");
                EnsureDirectory(ftpProfile.PublishUrl + "/.well-known", ftpProfile.UserName, ftpProfile.UserPWD);
                EnsureDirectory(ftpProfile.PublishUrl + "/.well-known/acme-challenge", ftpProfile.UserName, ftpProfile.UserPWD);
                using (var fs = File.OpenRead(answerPath))
                {
                    var fileName = Path.GetFileName(answerPath);
                    Upload(ftpProfile.PublishUrl + "/.well-known/acme-challenge/" + fileName, ftpProfile.UserName, ftpProfile.UserPWD, fs);
                }

                using (var ms = new MemoryStream())
                {
                    var sw = new StreamWriter(ms);
                    sw.Write(webConfig);
                    sw.Flush();
                    ms.Position = 0;
                    Upload(ftpProfile.PublishUrl + "/.well-known/acme-challenge/web.config", ftpProfile.UserName, ftpProfile.UserPWD, ms);
                }
            }

        }

        public override void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            var client = _webSiteManagementClient;
            Console.WriteLine(String.Format("Installing certificate {0} on azure", pfxFilename));
            var bytes = File.ReadAllBytes(pfxFilename);
            var pfx = Convert.ToBase64String(bytes);

            var s = client.Sites.GetSite(_resourceGroupname, _siteName);
            client.Certificates.CreateOrUpdateCertificate(_resourceGroupname, certificate.Subject.Replace("CN=", ""), new Certificate()
            {
                PfxBlob = pfx,
                Password = "",
                Location = s.Location,
            });
            var sslState = s.HostNameSslStates.FirstOrDefault(g => g.Name == target.Host);

            if (sslState == null)
            {
                sslState = new HostNameSslState()
                {
                    Name = target.Host,
                    SslState = SslState.SniEnabled,
                };
                s.HostNameSslStates.Add(sslState);
            }
            else
            {
                sslState.ToUpdate = true;
            }

            sslState.Thumbprint = certificate.Thumbprint;
            client.Sites.BeginCreateOrUpdateSite(_resourceGroupname, _siteName, s);

        }

        public override void PrintMenu()
        {
            if (!String.IsNullOrEmpty(Program.Options.ManualHost))
            {
                var target = new Target() { Host = Program.Options.ManualHost, WebRootPath = Program.Options.WebRoot, PluginName = Name };
                Program.Auto(target);
                Environment.Exit(0);
            }

            Console.WriteLine(" W: Configure Azure Web Site.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "w")
            {
                var settings = ActiveDirectoryServiceSettings.Azure;
                var authContext = new AuthenticationContext(settings.AuthenticationEndpoint + "common");

                var token = authContext.AcquireToken(settings.TokenAudience.ToString(), "1950a258-227b-4e31-a9cf-717495945fc2", new Uri("urn:ietf:wg:oauth:2.0:oob"), promptBehavior: PromptBehavior.Always);
                var creds = new TokenCredentials(token.AccessToken);
                var client = new SubscriptionClient(creds);
                client.SubscriptionId = Guid.NewGuid().ToString(); //Just set any GUID or subscription client will complain. 
                int i = 0;
                var subscriptions = client.Subscriptions.List();
                foreach (var sub in subscriptions)
                {
                    Console.WriteLine("["+i+"] "+ sub.DisplayName);
                    i++;
                }
                Console.WriteLine("Select subscription:");
                var subscription = subscriptions.ElementAt(Int32.Parse(Console.ReadLine()));

                var resClient = new ResourceManagementClient(creds);
                resClient.SubscriptionId = subscription.SubscriptionId;
                var resourceGroups = resClient.ResourceGroups.List();
                
                i = 0;
                foreach(var resourceGroup in resourceGroups)
                {
                    Console.WriteLine("[" + i + "] " + resourceGroup.Name);
                    i++;
                }

                Console.WriteLine("Select Resource Group");
                _resourceGroupname = resourceGroups.ElementAt(int.Parse(Console.ReadLine())).Name;

                _webSiteManagementClient = new WebSiteManagementClient(creds);
                _webSiteManagementClient.SubscriptionId = subscription.SubscriptionId;

                var sites = _webSiteManagementClient.Sites.GetSites(_resourceGroupname);
                i = 0;
                foreach(var site in sites.Value)
                {
                    Console.WriteLine("[" + i + "] " + site.Name);
                    i++;
                }

                Console.WriteLine("Select site");
                _siteName = sites.Value.ElementAt(int.Parse(Console.ReadLine())).Name;

                Console.Write("Enter a host name: ");
                var hostName = Console.ReadLine();

                // TODO: pull an existing host from the settings to default this value
                Console.Write("Enter a local folder path (local temp folder where the signature file will be saved before it is uploaded to the azure web site): ");
                var physicalPath = Console.ReadLine();
                
                

                // TODO: make a system where they can execute a program/batch file to update whatever they need after install.

                var target = new Target() { Host = hostName, WebRootPath = physicalPath, PluginName = Name };
                Program.Auto(target);
            }
        }

        private void Upload(string target, string user, string pass, Stream source)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(target);

            request.Method = WebRequestMethods.Ftp.UploadFile;
            request.Credentials = new NetworkCredential(user, pass);

            Stream requestStream = request.GetRequestStream();
            source.CopyTo(requestStream);
            requestStream.Close();

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Console.WriteLine("Upload File Complete, status {0}", response.StatusDescription);
            response.Close();
        }

        private void EnsureDirectory(string target, string user, string pass)
        {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(target);

            request.Method = WebRequestMethods.Ftp.MakeDirectory;
            request.Credentials = new NetworkCredential(user, pass);
            try
            {
                using (var response = request.GetResponse())
                {

                }
            }
            catch (Exception ex)
            {

            }
        }
    }

    public class PublishData
    {
        public IEnumerable<PublishProfile> PublishProfiles { get; private set; }
        public PublishData(Stream stream)
        {
            var profiles = new List<PublishProfile>();

            var xmldoc = new XmlDocument();
            xmldoc.Load(stream);

            foreach (XmlNode node in xmldoc.SelectNodes("//publishProfile"))
            {
                var profile = new PublishProfile()
                {
                    ProfileName = node.Attributes["profileName"].Value,
                    PublishMethod = node.Attributes["publishMethod"].Value,
                    PublishUrl = node.Attributes["publishUrl"].Value,
                    UserName = node.Attributes["userName"].Value,
                    UserPWD = node.Attributes["userPWD"].Value,
                };
                profiles.Add(profile);
            }
            PublishProfiles = profiles;
        }
    }

    public class PublishProfile
    {
        public string ProfileName { get; internal set; }
        public string PublishMethod { get; internal set; }
        public string PublishUrl { get; internal set; }
        public string UserName { get; internal set; }
        public string UserPWD { get; internal set; }
    }
}
