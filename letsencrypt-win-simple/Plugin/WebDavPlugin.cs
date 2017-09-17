using System;
using System.IO;
using System.Net;

namespace LetsEncrypt.ACME.Simple
{
    public class WebDavPlugin : ManualPlugin
    {
        private NetworkCredential WebDavCredentials
        {
            get
            {
                if (_WebDavCredentials == null)
                {
                    var ftpUser = Program.Input.RequestString("Enter the WebDav username");
                    var ftpPass = Program.Input.ReadPassword("Enter the WebDav password");
                    _WebDavCredentials = new NetworkCredential(ftpUser, ftpPass);
                }
                return _WebDavCredentials;
            }
        }
        private NetworkCredential _WebDavCredentials;

        public void Upload(string webDavPath, string content)
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

        public async void Delete(string webDavPath)
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

        public string GetFiles(string webDavPath)
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
    }
}