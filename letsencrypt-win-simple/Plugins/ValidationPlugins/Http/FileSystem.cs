using PKISharp.WACS.Clients;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using Microsoft.Web.Administration;
using System.IO;
using System.Linq;
using PKISharp.WACS.DomainObjects;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Classic FileSystem validation
    /// </summary>
    internal class FileSystemFactory : BaseHttpValidationFactory<FileSystem>
    {
        private IISClient _iisClient;

        public FileSystemFactory(IISClient iisClient, ILogService log) : base(log, nameof(FileSystem), "Save file on local (network) path")
        {
            _iisClient = iisClient;
        }

        public override bool ValidateWebroot(Target target) {
            return (string.IsNullOrEmpty(target.WebRootPath) && target.IIS == true) || target.WebRootPath.ValidPath(_log);
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            if (target.IIS == true && _iisClient.Version.Major > 0)
            {
                var validationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.ValidationSiteId), optionsService.Options.ValidationSiteId);
                if (validationSiteId != null)
                {
                    var site = _iisClient.GetWebSite(validationSiteId.Value); // Throws exception when not found
                    target.ValidationSiteId = validationSiteId;
                    target.WebRootPath = site.WebRoot();
                }
            }
            base.Default(target, optionsService);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            // Choose alternative site for validation
            if (target.IIS == true && _iisClient.Version.Major > 0)
            {
                if (inputService.PromptYesNo("Use different site for validation?"))
                {
                    var site = inputService.ChooseFromList("Validation site, must receive requests for all hosts on port 80",
                        _iisClient.WebSites,
                        x => new Choice<Site>(x) { Command = x.Id.ToString(), Description = x.Name }, true);
                    if (site != null)
                    {
                        target.ValidationSiteId = site.Id;
                        target.WebRootPath = site.WebRoot();
                    }
                }
            }
            base.Aquire(target, optionsService, inputService, runLevel);
        }
    }

    internal class FileSystem : BaseHttpValidation
    {
        protected IISClient _iisClient;

        public FileSystem(ScheduledRenewal renewal, Target target, IISClient iisClient, ILogService log, IInputService input, ProxyService proxy, string identifier) : 
            base(log, input, proxy, renewal, target, identifier)
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
            if (!_target.WebRootPathFrozen)
            {
                // IIS
                var siteId = _target.ValidationSiteId ?? _target.TargetSiteId;
                if (siteId > 0)
                {
                    var site = _iisClient.GetWebSite(siteId.Value); // Throws exception when not found
                    _iisClient.UpdateWebRoot(_target, site);
                }
            }
        }
    }
}
