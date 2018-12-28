using Microsoft.Web.Administration;
using PKISharp.WACS.Clients;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Classic FileSystem validation
    /// </summary>
    internal class FileSystemOptionsFactory : BaseHttpValidationOptionsFactory<FileSystem, FileSystemOptions>
    {
        public FileSystemOptionsFactory(IISClient iisClient, ILogService log) : base(log, iisClient) { }

        public override bool PathIsValid(string path) => path.ValidPath(_log);

        public override FileSystemOptions Default(Target target, IOptionsService optionsService)
        {
            var ret = new FileSystemOptions(BaseDefault(target, optionsService));
            if (target.IIS == true && _iisClient.HasWebSites)
            {
                var validationSiteId = optionsService.TryGetLong(nameof(optionsService.Options.ValidationSiteId), optionsService.Options.ValidationSiteId);
                if (validationSiteId != null)
                {
                    var site = _iisClient.GetWebSite(validationSiteId.Value); // Throws exception when not found
                    ret.Path = site.WebRoot();
                    ret.SiteId = validationSiteId;
                }
            }
            return ret;
        }

        public override FileSystemOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            // Choose alternative site for validation
            var ret = new FileSystemOptions(BaseAquire(target, optionsService, inputService, runLevel));
            if (target.IIS && _iisClient.HasWebSites && string.IsNullOrEmpty(ret.Path))
            {
                if (inputService.PromptYesNo("Use different site for validation?"))
                {
                    var site = inputService.ChooseFromList("Validation site, must receive requests for all hosts on port 80",
                        _iisClient.WebSites,
                        x => new Choice<Site>(x) { Command = x.Id.ToString(), Description = x.Name }, true);
                    if (site != null)
                    {
                        ret.Path = site.WebRoot();
                        ret.SiteId = site.Id;
                    }
                }
            }
            return ret;
        }
    }

}
