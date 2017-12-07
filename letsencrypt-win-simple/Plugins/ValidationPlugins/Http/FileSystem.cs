using ACMESharp;
using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Extensions;
using LetsEncrypt.ACME.Simple.Services;
using Microsoft.Web.Administration;
using System;
using System.IO;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins.Http
{
    class FileSystemFactory : HttpValidationFactory<FileSystem>
    {
        private IISClient _iisClient;
        private ILogService _log;
        public override bool ValidateWebroot(Target target) => target.WebRootPath.ValidPath(_log);

        public FileSystemFactory(IISClient iisClient, ILogService logService) : base(nameof(FileSystem), "Save file on local (network) path")
        {
            _iisClient = iisClient;
            _log = logService;
        }

        public override void Default(Target target, IOptionsService optionsService)
        {
            if (target.IIS == true && IISClient.Version.Major > 0)
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
            if (target.IIS == true && IISClient.Version.Major > 0)
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

    class FileSystem : HttpValidation
    {
        protected IISClient _iisClient;

        public FileSystem(ScheduledRenewal target, IISClient iisClient, ILogService logService, IInputService inputService, ProxyService proxyService) : 
            base(logService, inputService, proxyService, target)
        {
            _iisClient = iisClient;
        }

        public override void DeleteFile(string path)
        {
            (new FileInfo(path)).Delete();
        }

        public override void DeleteFolder(string path)
        {
            (new DirectoryInfo(path)).Delete();
        }

        public override bool IsEmpty(string path)
        {
            return (new DirectoryInfo(path)).GetFileSystemInfos().Count() == 0;
        }

        public override void WriteFile(string path, string content)
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
        public override void Refresh(Target scheduled)
        {
            // IIS
            var siteId = scheduled.ValidationSiteId ?? scheduled.TargetSiteId;
            if (siteId > 0)
            {
                var site = _iisClient.GetSite(siteId.Value); // Throws exception when not found
                _iisClient.UpdateWebRoot(scheduled, site);
            }
        }
    }
}
