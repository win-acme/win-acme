using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System.IO;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Classic FileSystem validation
    /// </summary>
    class FileSystemFactory : BaseHttpValidationFactory<FileSystem>
    {
        private IISClient _iisClient;

        public FileSystemFactory(IISClient iisClient, ILogService log) : base(log, nameof(FileSystem), "Save file on local (network) path")
        {
            _iisClient = iisClient;
        }

        public override bool ValidateWebroot(Target target) => target.WebRootPath.ValidPath(_log);

        public override void Default(Target target, IOptionsService optionsService)
        {
            if (target.IIS == true && _iisClient.Version.Major > 0)
            {
                var validationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.ValidationSiteId), optionsService.Options.ValidationSiteId);
                if (validationSiteId != null)
                {
                    var site = _iisClient.GetSite(validationSiteId.Value); // Throws exception when not found
                    target.ValidationSiteId = validationSiteId;
                    target.WebRootPath = site.WebRoot();
                }
            }
            base.Default(target, optionsService);
        }

        public override void Aquire(Target target, IOptionsService optionsService, IInputService inputService)
        {
            // Choose alternative site for validation
            if (target.IIS == true && _iisClient.Version.Major > 0)
            {
                if (inputService.PromptYesNo("Use different site for validation?"))
                {
                    var site = inputService.ChooseFromList("Validation site, must receive requests for all hosts on port 80",
                        _iisClient.RunningWebsites(),
                        x => new Choice<Site>(x) { Command = x.Id.ToString(), Description = x.Name }, true);
                    if (site != null)
                    {
                        target.ValidationSiteId = site.Id;
                        target.WebRootPath = site.WebRoot();
                    }
                }
            }
            base.Aquire(target, optionsService, inputService);
        }
    }

    class FileSystem : BaseHttpValidation
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
                di.Delete();
            }
            else
            {
                _log.Warning("Folder {path} already deleted", path);
            }
        }

        protected override bool IsEmpty(string path)
        {
            return (new DirectoryInfo(path)).GetFileSystemInfos().Count() == 0;
        }

        protected override void WriteFile(string path, string content)
        {
            var fi = new FileInfo(path);
            if (!fi.Directory.Exists)
            {
                fi.Directory.Create();
            }
            File.WriteAllText(path, content);
        }

        /// <summary>
        /// Update webroot 
        /// </summary>
        /// <param name="scheduled"></param>
        protected override void Refresh()
        {
            // IIS
            var siteId = _target.ValidationSiteId ?? _target.TargetSiteId;
            if (siteId > 0)
            {
                var site = _iisClient.GetSite(siteId.Value); // Throws exception when not found
                _iisClient.UpdateWebRoot(_target, site);
            }
        }
    }
}
