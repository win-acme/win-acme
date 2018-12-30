using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using System;
using System.IO;
using System.Linq;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class FileSystem : HttpValidation<FileSystemOptions, FileSystem>
    {
        protected IIISClient _iisClient;

        public FileSystem(FileSystemOptions options, IIISClient iisClient, HttpValidationParameters pars) : base(options, pars)
        {
            _iisClient = iisClient;
        }

        protected override void DeleteFile(string path)
        {
            var fi = new FileInfo(path);
            if (fi.Exists)
            {
                _log.Verbose("Deleting file {path}", path);
                fi.Delete();
            }
            else
            {
                _log.Warning("File {path} already deleted", path);
            }
        }

        protected override void DeleteFolder(string path)
        {
            var di = new DirectoryInfo(path);
            if (di.Exists)
            {
                _log.Verbose("Deleting folder {path}", path);
                di.Delete();
            }
            else
            {
                _log.Warning("Folder {path} already deleted", path);
            }
        }

        protected override bool IsEmpty(string path)
        {
            return !(new DirectoryInfo(path)).GetFileSystemInfos().Any();
        }

        protected override void WriteFile(string path, string content)
        {
            var fi = new FileInfo(path);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            _log.Verbose("Writing file to {path}", path);
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Update webroot 
        /// </summary>
        /// <param name="scheduled"></param>
        protected override void Refresh()
        {
            if (string.IsNullOrEmpty(_options.Path))
            {
                // Update web root path
                var siteId = _options.SiteId ?? _targetPart.SiteId;
                if (siteId > 0)
                {
                    _path = _iisClient.GetWebSite(siteId.Value).Path;
                }
                else
                {
                    throw new Exception("No path specified");
                }
            }
            else
            {
                _path = _options.Path;
            }
        }
    }
}
