using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Serilog;

namespace LetsEncrypt.ACME.Simple
{
    public class WebDavPlugin : Plugin
    {
        public override string Name => "WebDav";

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
            Console.WriteLine(" WARNING: Installing is not supported for the Web Dav Plugin.");
        }

        public override void Install(Target target)
        {
            // This method with just the Target paramater is currently only used by Centralized SSL
            Console.WriteLine(" WARNING: Central SSL is not supported for the Web Dav Plugin.");
        }

        public override void Renew(Target target)
        {
            var auth = Program.Authorize(target);
            if (auth.Status == "valid")
            {
                var pfxFilename = Program.GetCertificate(target);
                Console.WriteLine("");
                Console.WriteLine($"You can find the certificate at {pfxFilename}");
                Log.Information("You can find the certificate at {pfxFilename}");
            }
        }

        public override void PrintMenu()
        {
            Console.WriteLine(" W: Generate a certificate via WebDav and install it manually.");
        }

        public override void HandleMenuResponse(string response, List<Target> targets)
        {
            if (response == "w")
            {
                Console.Write("Enter a host name: ");
                var hostName = Console.ReadLine();
                string[] alternativeNames = null;

                if (Program.Options.San)
                {
                    Console.Write("Enter all Alternative Names seperated by a comma ");
                    Console.SetIn(new System.IO.StreamReader(Console.OpenStandardInput(8192)));
                    var sanInput = Console.ReadLine();
                    alternativeNames = sanInput.Split(',');
                }
                Console.WriteLine("Enter a site path (the web root of the host for http authentication)");
                Console.WriteLine("Note: Password cannot have a : / or @ in it");
                Console.WriteLine("Example, http://user:password@domain.com:80/");
                Console.WriteLine("Example, https://user:password@domain.com:443/");
                Console.Write(": ");
                var webDavPath = Console.ReadLine();

                List<string> sanList = new List<string>();

                if (alternativeNames != null)
                {
                    sanList = new List<string>(alternativeNames);
                }
                if (sanList.Count <= 100)
                {
                    var target = new Target()
                    {
                        Host = hostName,
                        WebRootPath = webDavPath,
                        PluginName = Name,
                        AlternativeNames = sanList
                    };
                    var auth = Program.Authorize(target);
                    if (auth.Status == "valid")
                    {
                        var pfxFilename = Program.GetCertificate(target);
                        Console.WriteLine("");
                        Console.WriteLine($"You can find the certificate at {pfxFilename}");
                        Log.Information("You can find the certificate at {pfxFilename}");
                    }
                }
                else
                {
                    Console.WriteLine(
                        $" You entered too many hosts for a SAN certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                    Log.Error(
                        "You entered too many hosts for a San certificate. Let's Encrypt currently has a maximum of 100 alternative names per certificate.");
                }
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            Console.WriteLine($" Writing challenge answer to {answerPath}");
            Log.Information("Writing challenge answer to {answerPath}", answerPath);

            Upload(answerPath, fileContents);
        }

        private void Upload(string webDavPath, string content)
        {
            Uri webDavUri = new Uri(webDavPath);
            Log.Verbose("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            int pathLastSlash = webDavUri.AbsolutePath.LastIndexOf("/") + 1;
            string file = webDavUri.AbsolutePath.Substring(pathLastSlash);
            string path = webDavUri.AbsolutePath.Remove(pathLastSlash);
            Log.Verbose("webDavConnection {@webDavConnection}", webDavConnection);

            Log.Verbose("UserInfo {@UserInfo}", webDavUri.UserInfo);
            int userIndex = webDavUri.UserInfo.IndexOf(":");

            string user = webDavUri.UserInfo.Remove(userIndex, (webDavUri.UserInfo.Length - userIndex));
            Log.Verbose("user {@user}", user);

            string pass = webDavUri.UserInfo.Substring(userIndex + 1);
            Log.Verbose("pass {@pass}", pass);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            Log.Verbose("stream {@stream}", stream);

            var client = new WebDAVClient.Client(new NetworkCredential {UserName = user, Password = pass});
            client.Server = webDavConnection;
            client.BasePath = path;

            var fileUploaded = client.Upload("/", stream, file).Result;

            Console.WriteLine($"Upload Status {fileUploaded}");
            Log.Information("Upload Status {StatusDescription}", fileUploaded);
        }

        private async void Delete(string webDavPath)
        {
            Uri webDavUri = new Uri(webDavPath);
            Log.Verbose("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            string path = webDavUri.AbsolutePath;
            Log.Verbose("webDavConnection {@webDavConnection}", webDavConnection);

            Log.Verbose("UserInfo {@UserInfo}", webDavUri.UserInfo);
            int userIndex = webDavUri.UserInfo.IndexOf(":");

            string user = webDavUri.UserInfo.Remove(userIndex, (webDavUri.UserInfo.Length - userIndex));
            Log.Verbose("user {@user}", user);

            string pass = webDavUri.UserInfo.Substring(userIndex + 1);
            Log.Verbose("pass {@pass}", pass);

            var client = new WebDAVClient.Client(new NetworkCredential {UserName = user, Password = pass});
            client.Server = webDavConnection;
            client.BasePath = path;

            try
            {
                await client.DeleteFile(path);
            }
            catch (Exception ex)
            {
                Log.Warning("Error deleting file/ folder {@ex}", ex);
            }

            string result = "N/A";

            Console.WriteLine($"Delete Status {result}");
            Log.Information("Delete Status {StatusDescription}", result);
        }

        private string GetFiles(string webDavPath)
        {
            Uri webDavUri = new Uri(webDavPath);
            Log.Verbose("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            string path = webDavUri.AbsolutePath;
            Log.Verbose("webDavConnection {@webDavConnection}", webDavConnection);

            Log.Verbose("UserInfo {@UserInfo}", webDavUri.UserInfo);
            int userIndex = webDavUri.UserInfo.IndexOf(":");

            string user = webDavUri.UserInfo.Remove(userIndex, (webDavUri.UserInfo.Length - userIndex));
            Log.Verbose("user {@user}", user);

            string pass = webDavUri.UserInfo.Substring(userIndex + 1);
            Log.Verbose("pass {@pass}", pass);

            var client = new WebDAVClient.Client(new NetworkCredential {UserName = user, Password = pass});
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

            Console.WriteLine($" Writing web.config to add extensionless mime type to {webConfigPath}");
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