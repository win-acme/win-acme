using LetsEncrypt.ACME.Simple.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace LetsEncrypt.ACME.Simple.Client
{
    class WebDavClient
    {
        private NetworkCredential WebDavCredentials { get; set; }

        public WebDavClient(WebDavOptions options)
        {
            WebDavCredentials = options.GetCredential();
        }

        private WebDAVClient.Client GetClient(string webDavPath)
        {
            Uri webDavUri = new Uri(webDavPath);
            var scheme = webDavUri.Scheme;
            string webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            var client = new WebDAVClient.Client(WebDavCredentials);
            client.Server = webDavConnection;
            client.BasePath = webDavUri.AbsolutePath;
            return client;
        }

        public void Upload(string webDavPath, string content)
        {
            try
            {
                using (MemoryStream stream = new MemoryStream())
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Position = 0;
                    int pathLastSlash = webDavPath.LastIndexOf("/") + 1;
                    var file = webDavPath.Substring(pathLastSlash);
                    var path = webDavPath.Remove(pathLastSlash);
                    var client = GetClient(path);
                    var fileUploaded = client.Upload("/", stream, file).Result;
                    Program.Log.Verbose("Upload status {StatusDescription}", fileUploaded);
                }
            }
            catch (Exception ex)
            {
                Program.Log.Verbose("WebDav error {@ex}", ex);
                Program.Log.Warning("Error uploading file {webDavPath} {Message}", webDavPath, ex.Message);
            }

        }

        public async void Delete(string webDavPath)
        {
            var client = GetClient(webDavPath);
            try
            {
                await client.DeleteFile(webDavPath);
            }
            catch (Exception ex)
            {
                Program.Log.Verbose("WebDav error {@ex}", ex);
                Program.Log.Warning("Error deleting file/folder {webDavPath} {Message}", webDavPath, ex.Message);
            }
        }

        public string GetFiles(string webDavPath)
        {
            try
            {
                var client = GetClient(webDavPath);
                var folderFiles = client.List().Result;
                var names = string.Join(",", folderFiles.Select(x => x.DisplayName.Trim()).ToArray());
                Program.Log.Verbose("Files in path {webDavPath}: {@names}", webDavPath, names);
                return names;
            }
            catch (Exception ex)
            {
                Program.Log.Verbose("WebDav error {@ex}", ex);
                return string.Empty;
            }
        }
    }
}