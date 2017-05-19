using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using System.Security;

namespace LetsEncrypt.ACME.Simple
{
    internal class WebDavPlugin : Plugin
    {
        private string hostName;
        
        private string webDavPath;
        
        private NetworkCredential WebDavCredentials { get; set; }

        public override string Name => "WebDav";

        public override bool RequiresElevated => true;

        public override bool GetSelected(ConsoleKeyInfo key) => key.Key == ConsoleKey.W;

        public override bool Validate() => true;

        public override bool SelectOptions(Options options)
        {
            Console.Write("Enter a host name: ");
            hostName = Console.ReadLine();

            Console.WriteLine("Enter a site path for WebDav authentication");
            Console.WriteLine("Example, http://example.com:80/");
            Console.WriteLine("Example, https://example.com:443/");
            Console.Write(": ");
            webDavPath = Console.ReadLine();

            Console.Write("Enter the WebDAV username: ");
            var webDavUser = Console.ReadLine();

            Console.Write("Enter the WebDAV password: ");
            var webDavPass = LetsEncrypt.ReadPassword();

            WebDavCredentials = new NetworkCredential(webDavUser, webDavPass);

            return true;
        }

        public override List<Target> GetTargets()
        {
            var result = new List<Target>();
            result.Add(new Target
            {
                Host = hostName,
                WebRootPath = webDavPath,
                PluginName = Name,
                AlternativeNames = AlternativeNames
            });
            return result;
        }

        public override void Renew(Target target, Options options)
        {
            Install(target, options);
        }

        public override void Install(Target target, Options options)
        {
            Auto(target, options);
        }

        public override void PrintMenu()
        {
            Console.WriteLine(" W: Generate a certificate via WebDav and install it manually.");
        }
        
        public override string Auto(Target target, Options options)
        {
            string pfxFilename = null;
            if (WebDavCredentials != null)
            {
                pfxFilename = base.Auto(target, options);
                if (!string.IsNullOrEmpty(pfxFilename))
                {
                    Console.WriteLine("");
                    Log.Information("You can find the certificate at {pfxFilename}", pfxFilename);
                }
            }
            else
            {
                Console.WriteLine("The Web Dav Credentials are not set. Please specify them and try again.");
            }
            return pfxFilename;
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
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

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            answerPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
            var webConfigPath = Path.Combine(answerPath, "web.config");
            
            Log.Information("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);

            Upload(webConfigPath, File.ReadAllText(_sourceFilePath));
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Console.WriteLine(" Deleting answer");
            Log.Information("Deleting answer");
            Delete(answerPath);

            try
            {
                if (Properties.Settings.Default.CleanupFolders == true)
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