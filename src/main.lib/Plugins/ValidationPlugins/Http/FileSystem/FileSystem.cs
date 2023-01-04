using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Capabilities;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    [IPlugin.Plugin<
        FileSystemOptions, FileSystemOptionsFactory, 
        HttpValidationCapability, WacsJsonPlugins>
        ("1c77b3a4-5310-4c46-92c6-00d866e84d6b", 
        "FileSystem", "Save verification files on (network) path")]
    internal class FileSystem : HttpValidation<FileSystemOptions>
    {
        protected IIISClient _iisClient;

        public FileSystem(FileSystemOptions options, IIISClient iisClient, RunLevel runLevel, HttpValidationParameters pars) : base(options, runLevel, pars) => _iisClient = iisClient;

        protected override Task DeleteFile(string path)
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
            return Task.CompletedTask;
        }

        protected override Task DeleteFolder(string path)
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
            return Task.CompletedTask;
        }

        protected override Task<bool> IsEmpty(string path)
        {
            var x = new DirectoryInfo(path);
            return Task.FromResult(x.Exists && !x.EnumerateFileSystemInfos().Any());
        }

        protected override async Task WriteFile(string path, string content)
        {
            var fi = new FileInfo(path);
            if (fi.Directory != null && !fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            _log.Verbose("Writing file to {path}", path);
            await File.WriteAllTextAsync(path, content);
        }

        /// <summary>
        /// Update webroot 
        /// </summary>
        /// <param name="scheduled"></param>
        protected override void Refresh(TargetPart targetPart)
        {
            if (string.IsNullOrEmpty(_options.Path))
            {
                // Update web root path
                var siteId = _options.SiteId ?? targetPart.SiteId;
                if (siteId > 0)
                {
                    _path = _iisClient.GetSite(siteId.Value, IISSiteType.Web).Path;
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
