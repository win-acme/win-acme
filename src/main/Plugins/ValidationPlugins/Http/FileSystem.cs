using PKISharp.WACS.Clients;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using Microsoft.Web.Administration;
using System.IO;
using System.Linq;
using PKISharp.WACS.DomainObjects;
using System;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    internal class FileSystem : HttpValidation<FileSystemOptions, FileSystem>
    {
        protected IISClient _iisClient;

        public FileSystem(
            ScheduledRenewal renewal, 
            TargetPart target, 
            IISClient iisClient, 
            ILogService log,
            FileSystemOptions options,
            IInputService input, 
            ProxyService proxy, 
            string identifier) : 
            base(log, input, options, proxy, renewal, target, identifier)
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
            // Update web root path
            var siteId = _options.SiteId ?? _target.SiteId;
            if (siteId > 0)
            {
                _path = _iisClient.GetWebSite(siteId.Value).WebRoot();
                if (!string.IsNullOrEmpty(_options.Path))
                {
                    if (!string.Equals(_options.Path, _path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _log.Warning("- Change path from {old} to {new}", _options.Path, _path);
                        _options.Path = _path;
                    }
                }
            }
        }
    }
}
