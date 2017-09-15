using System;
using System.IO;
using System.Net;

namespace LetsEncrypt.ACME.Simple
{
    public class WebDavPlugin : ManualPlugin
    {
        private NetworkCredential WebDavCredentials { get; set; }

        public override string Name => "WebDav";

        public override void Renew(Target target)
        {
            Program.Log.Warning("Renewal is not supported for the Web Dav Plugin.");
        }

        public override string MenuOption => "W";
        public override string Description => "Generate a certificate via WebDav and install it manually.";

        public override void Run()
        {
            var target = InputTarget(Name, new[] {
                "Enter a site path (the web root of the host for http authentication)",
                " Example, http://domain.com:80/",
                " Example, https://domain.com:443/"
            });
            if (target != null)
            {
                var webDavUser = Program.Input.RequestString("Enter the WebDAV username");
                var webDavPass = Program.Input.ReadPassword("Enter the WebDAV password");
                WebDavCredentials = new NetworkCredential(webDavUser, webDavPass);
                Auto(target);
            }
        }

        public override void Auto(Target target)
        {
            if (WebDavCredentials != null)
            {
                var auth = Program.Authorize(target);
                if (auth.Status == "valid")
                {
                    var pfxFilename = Program.GetCertificate(target);
                    Program.Log.Information("You can find the certificate at {pfxFilename}", pfxFilename);
                }
            }
            else
            {
                Program.Log.Error("The Web Dav Credentials are not set. Please specify them and try again.");
            }
        }

        public override void CreateAuthorizationFile(string answerPath, string fileContents)
        {
            answerPath = answerPath.Replace('\\', '/');
            Program.Log.Debug("Writing challenge answer to {answerPath}", answerPath);
            Upload(answerPath, fileContents);
        }

        private void Upload(string webDavPath, string content)
        {
            Uri webDavUri = new Uri(webDavPath);
            Program.Log.Debug("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            int pathLastSlash = webDavUri.AbsolutePath.LastIndexOf("/") + 1;
            string file = webDavUri.AbsolutePath.Substring(pathLastSlash);
            string path = webDavUri.AbsolutePath.Remove(pathLastSlash);
            Program.Log.Debug("webDavConnection {@webDavConnection}", webDavConnection);

            Program.Log.Debug("UserName {@UserName}", WebDavCredentials.UserName);

            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(content);
            writer.Flush();
            stream.Position = 0;

            Program.Log.Debug("stream {@stream}", stream);

            var client = new WebDAVClient.Client(WebDavCredentials);
            client.Server = webDavConnection;
            client.BasePath = path;

            var fileUploaded = client.Upload("/", stream, file).Result;

            Program.Log.Information("Upload Status {StatusDescription}", fileUploaded);
        }

        private async void Delete(string webDavPath)
        {
            Uri webDavUri = new Uri(webDavPath);
            Program.Log.Debug("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            string path = webDavUri.AbsolutePath;
            Program.Log.Debug("webDavConnection {@webDavConnection}", webDavConnection);

            Program.Log.Debug("UserName {@UserName}", WebDavCredentials.UserName);

            var client = new WebDAVClient.Client(WebDavCredentials);
            client.Server = webDavConnection;
            client.BasePath = path;

            try
            {
                await client.DeleteFile(path);
            }
            catch (Exception ex)
            {
                Program.Log.Warning("Error deleting file/folder {@ex}", ex);
            }

            string result = "N/A";

            Program.Log.Information("Delete Status {StatusDescription}", result);
        }

        private string GetFiles(string webDavPath)
        {
            Uri webDavUri = new Uri(webDavPath);
            Program.Log.Debug("webDavUri {@webDavUri}", webDavUri);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            string path = webDavUri.AbsolutePath;
            Program.Log.Debug("webDavConnection {@webDavConnection}", webDavConnection);

            Program.Log.Debug("UserName {@UserName}", WebDavCredentials.UserName);

            var client = new WebDAVClient.Client(WebDavCredentials);
            client.Server = webDavConnection;
            client.BasePath = path;

            var folderFiles = client.List().Result;
            string names = "";
            foreach (var file in folderFiles)
            {
                names = names + file.DisplayName + ",";
            }

            Program.Log.Debug("Files {@names}", names);
            return names.TrimEnd('\r', '\n', ',');
        }

        private readonly string _sourceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web_config.xml");

        public override void BeforeAuthorize(Target target, string answerPath, string token)
        {
            answerPath = answerPath.Remove((answerPath.Length - token.Length), token.Length);
            var webConfigPath = Path.Combine(answerPath, "web.config");

            Program.Log.Debug("Writing web.config to add extensionless mime type to {webConfigPath}", webConfigPath);

            Upload(webConfigPath, File.ReadAllText(_sourceFilePath));
        }

        public override void DeleteAuthorization(string answerPath, string token, string webRootPath, string filePath)
        {
            Program.Log.Debug("Deleting answer");
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
                            Program.Log.Debug("Deleting web.config");
                            Delete(folderPath + "web.config");
                            Program.Log.Debug("Deleting {folderPath}", folderPath);
                            Delete(folderPath);
                            var filePathFirstDirectory =
                                Environment.ExpandEnvironmentVariables(Path.Combine(webRootPath,
                                    filePath.Remove(filePath.IndexOf("/"), (filePath.Length - filePath.IndexOf("/")))));
                            Program.Log.Debug("Deleting {filePathFirstDirectory}", filePathFirstDirectory);
                            Delete(filePathFirstDirectory);
                        }
                        else
                        {
                            Program.Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                        }
                    }
                    else
                    {
                        Program.Log.Warning("Additional files exist in {folderPath} not deleting.", folderPath);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.Log.Warning("Error occured while deleting folder structure. Error: {@ex}", ex);
            }
        }
    }
}