using PKISharp.WACS.Configuration;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using WebDav;

namespace PKISharp.WACS.Client
{
    internal class WebDavClientWrapper : IDisposable
    {
        private readonly NetworkCredential? _credential;
        private readonly ILogService _log;
        private readonly IProxyService _proxy;
        private readonly WebDavClient _client;
        public WebDavClientWrapper(
            NetworkCredentialOptions? options, 
            ILogService log, 
            IProxyService proxy, 
            SecretServiceManager secretService)
        {
            _log = log;
            if (options != null && options.UserName != null)
            {
                _credential = options.GetCredential(secretService);
            }
            _proxy = proxy;
            _client = new WebDavClient(new WebDavClientParams()
            {
                Proxy = _proxy.GetWebProxy(),
                UseDefaultCredentials = _proxy.ProxyType == WindowsProxyUsePolicy.UseWinInetProxy,
                Credentials = _credential
            });
        }

        private string NormalizePath(string path)
        {
            return path.
                Replace("webdav:", "https:").
                Replace("dav:", "https:").
                Replace("\\\\", "https://").
                Replace("\\", "/");
        }

        public void Upload(string originalPath, string content)
        {
            try
            {
                var path = NormalizePath(originalPath);
                var uri = new Uri(path);
                var stream = new MemoryStream();
                using var writer = new StreamWriter(stream);
                writer.Write(content);
                writer.Flush();
                stream.Position = 0;
                var currentPath = $"{uri.Scheme}://{uri.Host}{(uri.IsDefaultPort ? "" : $":{uri.Port}")}";
                var directories = uri.AbsolutePath.Trim('/').Split('/');
                for (var i = 0; i < directories.Length - 1; i++)
                {
                    currentPath += $"/{directories[i]}";
                    if (!FolderExists(currentPath))
                    {
                        var dirCreated = _client.Mkcol(currentPath).Result;
                        if (!dirCreated.IsSuccessful)
                        {
                            throw new Exception($"path {currentPath} - {dirCreated.StatusCode} ({dirCreated.Description})");
                        }
                    }
                }
                // Upload file
                currentPath += $"/{directories[directories.Count() - 1]}";
                var fileUploaded = _client.PutFile(currentPath, stream).Result;
                if (!fileUploaded.IsSuccessful)
                {
                    throw new Exception($"{fileUploaded.StatusCode} ({fileUploaded.Description})");
                }
            }
            catch (Exception ex)
            {
                _log.Error("Error uploading file {path} {Message}", originalPath, ex.Message);
                throw;
            }
        }

        private bool FolderExists(string path)
        {
            var exists = _client.Propfind(path).Result;
            return exists.IsSuccessful &&
                exists.Resources.Any() &&
                exists.Resources.First().IsCollection;
        }

        internal bool IsEmpty(string path)
        {
            var exists = _client.Propfind(path).Result;
            return exists.IsSuccessful &&
                !exists.Resources.Any();
        }

        public void Delete(string path)
        {
            path = NormalizePath(path);
            try
            {
                var x = _client.Delete(path).Result;
            }
            catch (Exception ex)
            {
                _log.Verbose("WebDav error {@ex}", ex);
                _log.Warning("Error deleting file/folder {path} {Message}", path, ex.Message);
            }
        }

        public IEnumerable<string> GetFiles(string path)
        {
            try
            {
                path = NormalizePath(path);
                var folderFiles = _client.Propfind(path).Result;
                if (folderFiles.IsSuccessful)
                {
                    return folderFiles.Resources.Select(r => r.DisplayName);
                }
            }
            catch (Exception ex)
            {
                _log.Verbose("WebDav error {@ex}", ex);
            }
            return new string[] { };
        }

        #region IDisposable

        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_client != null)
                    {
                        _client.Dispose();
                    }
                }
                disposedValue = true;
            }
        }

        public void Dispose() => Dispose(true);

        #endregion
    }
}