using Autofac;
using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace PKISharp.WACS.Client
{
    internal class WebDavClient
    {
        private NetworkCredential _credential { get; set; }
        private ILogService _log;

        public WebDavClient(WebDavOptions options, ILogService log)
        {
            _log = log;
            _credential = options.GetCredential();
        }

        private WebDAVClient.Client GetClient(string webDavPath)
        {
            var webDavUri = new Uri(webDavPath);
            var scheme = webDavUri.Scheme;
            var webDavConnection = scheme + "://" + webDavUri.Host + ":" + webDavUri.Port;
            var client = new WebDAVClient.Client(_credential)
            {
                Server = webDavConnection,
                BasePath = webDavUri.AbsolutePath
            };
            return client;
        }

        public void Upload(string webDavPath, string content)
        {
            try
            {
                using (var stream = new MemoryStream())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(content);
                    writer.Flush();
                    stream.Position = 0;
                    var pathLastSlash = webDavPath.LastIndexOf("/", StringComparison.Ordinal) + 1;
                    var file = webDavPath.Substring(pathLastSlash);
                    var path = webDavPath.Remove(pathLastSlash);
                    var client = GetClient(path);
                    var fileUploaded = client.Upload("/", stream, file).Result;
                    _log.Verbose("Upload status {StatusDescription}", fileUploaded);
                }
            }
            catch (Exception ex)
            {
                _log.Verbose("WebDav error {@ex}", ex);
                _log.Warning("Error uploading file {webDavPath} {Message}", webDavPath, ex.Message);
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
                _log.Verbose("WebDav error {@ex}", ex);
                _log.Warning("Error deleting file/folder {webDavPath} {Message}", webDavPath, ex.Message);
            }
        }

        public string GetFiles(string webDavPath)
        {
            try
            {
                var client = GetClient(webDavPath);
                var folderFiles = client.List().Result;
                var names = string.Join(",", folderFiles.Select(x => x.DisplayName.Trim()).ToArray());
                _log.Verbose("Files in path {webDavPath}: {@names}", webDavPath, names);
                return names;
            }
            catch (Exception ex)
            {
                _log.Verbose("WebDav error {@ex}", ex);
                return string.Empty;
            }
        }
    }
}