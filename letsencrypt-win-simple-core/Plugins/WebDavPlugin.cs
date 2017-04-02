using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using LetsEncrypt.ACME.Simple.Core.Configuration;
using LetsEncrypt.ACME.Simple.Core.Interfaces;
using Serilog;

namespace LetsEncrypt.ACME.Simple.Core.Plugins
{
    public class WebDavPlugin : IPlugin
    {
        protected IOptions Options;
        protected ILetsEncryptService LetsEncryptService;
        protected IConsoleService ConsoleService;
        public WebDavPlugin(IOptions options, ILetsEncryptService letsEncryptService, 
            IConsoleService consoleService)
        {
            Options = options;
            LetsEncryptService = letsEncryptService;
            ConsoleService = consoleService;
        }

        private NetworkCredential WebDavCredentials { get; set; }

        public string Name => "WebDav";

        public List<Target> GetTargets()
        {
            var result = new List<Target>();

            return result;
        }

        public List<Target> GetSites()
        {
            var result = new List<Target>();

            return result;
        }

        public void OnAuthorizeFail(Target target)
        {
        }

        public void Install(Target target, string pfxFilename, X509Store store, X509Certificate2 certificate)
        {
            if (!string.IsNullOrWhiteSpace(Options.Script) &&
                !string.IsNullOrWhiteSpace(Options.ScriptParameters))
            {
                var parameters = string.Format(Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword,
                    pfxFilename, store.Name, certificate.FriendlyName, certificate.Thumbprint);
                Log.Information("Running {Script} with {parameters}", Options.Script, parameters);
                Process.Start(Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Options.Script))
            {
                Log.Information("Running {Script}", Options.Script);
                Process.Start(Options.Script);
            }
            else
            {
                Log.Warning(" WARNING: Unable to configure server software.");
            }
        }

        public void Install(Target target)
        {
            // This method with just the Target paramater is currently only used by Centralized SSL
            if (!string.IsNullOrWhiteSpace(Options.Script) &&
                !string.IsNullOrWhiteSpace(Options.ScriptParameters))
            {
                var parameters = string.Format(Options.ScriptParameters, target.Host,
                    Properties.Settings.Default.PFXPassword, Options.CentralSslStore);
                Log.Information("Running {Script} with {parameters}", Options.Script, parameters);
                Process.Start(Options.Script, parameters);
            }
            else if (!string.IsNullOrWhiteSpace(Options.Script))
            {
                Log.Information("Running {Script}", Options.Script);
                Process.Start(Options.Script);
            }
            else
            {
                Log.Warning(" WARNING: Unable to configure server software.");
            }
        }

        public void Renew(Target target)
        {
            Log.Warning(" WARNING: Renewal is not supported for the Web Dav Plugin.");
        }

        public void PrintMenu()
        {
            ConsoleService.WriteLine(" W: Generate a certificate via WebDav and install it manually.");
        }

        public void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "w")
            {
                ConsoleService.Write("Enter a host name: ");
                var hostName = ConsoleService.ReadLine();
                string[] alternativeNames = null;

                if (Options.San)
                    alternativeNames = ConsoleService.GetSanNames();
                ConsoleService.WriteLine("Enter a site path (the web root of the host for http authentication)");
                ConsoleService.WriteLine("Example, http://domain.com:80/");
                ConsoleService.WriteLine("Example, https://domain.com:443/");
                ConsoleService.Write(": ");
                var webDavPath = ConsoleService.ReadLine();

                ConsoleService.Write("Enter the WebDAV username: ");
                var webDavUser = ConsoleService.ReadLine();

                ConsoleService.Write("Enter the WebDAV password: ");
                var webDavPass = ConsoleService.ReadPassword();

                WebDavCredentials = new NetworkCredential(webDavUser, webDavPass);

                List<string> sanList = new List<string>();

                if (alternativeNames != null)
                {
                    sanList = new List<string>(alternativeNames);
                }
                if (sanList.Count <= 100)
                {
                    var target = new Target
                    {
                        Host = hostName,
                        WebRootPath = webDavPath,
                        PluginName = Name,
                        AlternativeNames = sanList
                    };
                    Auto(target);
                }
                else
                {
                    Log.Error(
                        "You entered too many hosts for a San certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                }
            }
        }

        public void Auto(Target target)
        {
            if (WebDavCredentials != null)
            {
                var auth = LetsEncryptService.Authorize(target);
                if (auth.Status == "valid")
                {
                    var pfxFilename = LetsEncryptService.GetCertificate(target);
                    ConsoleService.WriteLine("");
                    Log.Information("You can find the certificate at {pfxFilename}", pfxFilename);
                }
            }
            else
            {
                Log.Warning("The Web Dav Credentials are not set. Please specify them and try again.");
            }
        }

        public void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Log.Information("Writing challenge answer to {answerPath}", answerPath);
            Upload(answerPath, fileContents);
        }

        private void Upload(string webDavPath, string content)
        {
            Uri webDavUri = new Uri(webDavPath);
            Log.Debug("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            int pathLastSlash = webDavUri.AbsolutePath.LastIndexOf("/") + 1;
            string file = webDavUri.AbsolutePath.Substring(pathLastSlash);
            string path = webDavUri.AbsolutePath.Remove(pathLastSlash);
            Log.Debug("webDavConnection {@webDavConnection}", webDavConnection);

            Log.Debug("UserName {@UserName}", WebDavCredentials.UserName);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            Log.Debug("stream {@stream}", stream);

            var client = new WebDAVClient.Client(WebDavCredentials);
            client.Server = webDavConnection;
            client.BasePath = path;

            var fileUploaded = client.Upload("/", stream, file).Result;
            
            Log.Information("Upload Status {StatusDescription}", fileUploaded);
        }

        private async void Delete(string webDavPath)
        {
            Uri webDavUri = new Uri(webDavPath);
            Log.Debug("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            string path = webDavUri.AbsolutePath;
            Log.Debug("webDavConnection {@webDavConnection}", webDavConnection);

            Log.Debug("UserName {@UserName}", WebDavCredentials.UserName);

            var client = new WebDAVClient.Client(WebDavCredentials);
            client.Server = webDavConnection;
            client.BasePath = path;

            try
            {
                await client.DeleteFile(path);
            }
            catch (Exception ex)
            {
                Log.Warning("Error deleting file/folder {@ex}", ex);
            }

            string result = "N/A";
            
            Log.Information("Delete Status {StatusDescription}", result);
        }

        private string GetFiles(string webDavPath)
        {
            Uri webDavUri = new Uri(webDavPath);
            Log.Debug("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            string path = webDavUri.AbsolutePath;
            Log.Debug("webDavConnection {@webDavConnection}", webDavConnection);

            Log.Debug("UserName {@UserName}", WebDavCredentials.UserName);

            var client = new WebDAVClient.Client(WebDavCredentials);
            client.Server = webDavConnection;
            client.BasePath = path;

            var folderFiles = client.List().Result;
            string names = "";
            foreach (var file in folderFiles)
            {
                names = names + file.DisplayName + ",";
            }

            Log.Debug("Files {@names}", names);
            return names.TrimEnd('\r', '\n', ',');
        }

        private readonly string _sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");

        public void BeforeAuthorize(Target target, string answerPath, string token)
        {
            answerPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
            var webConfigPath = Path.Combine(answerPath, "web.config");
            
            Log.Information("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);

            Upload(webConfigPath, File.ReadAllText(_sourceFilePath));
        }

        public void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Log.Information("Deleting answer");
            Delete(answerPath);

            try
            {
                if (Properties.Settings.Default.CleanupFolders)
                {
                    var folderPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
                    var files = GetFiles(folderPath);

                    if (!string.IsNullOrWhiteSpace(files))
                    {
                        if (files == "web.config")
                        {
                            Log.Information("Deleting web.config");
                            Delete(folderPath + "web.config");
                            Log.Information("Deleting {folderPath}", folderPath);
                            Delete(folderPath);
                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath,
                                    filePath.Remove(filePath.IndexOf("/"), (filePath.Length - filePath.IndexOf("/")))));
                            Log.Information("Deleting {filePathFirstDirectory}", filePathFirstDirectory);
                            Delete(filePathFirstDirectory);
                        }
                        else
                        {
                            Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                        }
                    }
                    else
                    {
                        Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }
    }
}