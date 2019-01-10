using Microsoft.Web.Administration;
using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Classic FileSystem validation
    /// </summary>
    internal class FileSystemOptionsFactory : HttpValidationOptionsFactory<FileSystem, FileSystemOptions>
    {
        private IIISClient _iisClient;

        public FileSystemOptionsFactory(IIISClient iisClient, ILogService log) : base(log)
        {
            _iisClient = iisClient;
        }
        public override bool PathIsValid(string path) => path.ValidPath(_log);
        public override bool AllowEmtpy(Target target) => target.IIS;

        public override FileSystemOptions Default(Target target, IOptionsService optionsService)
        {
            var args = optionsService.GetArguments<FileSystemArguments>();
            var ret = new FileSystemOptions(BaseDefault(target, args, optionsService));
            if (target.IIS && _iisClient.HasWebSites)
            {
                
                if (args.ValidationSiteId != null)
                {
                    // Throws exception when not found
                    var site = _iisClient.GetWebSite(args.ValidationSiteId.Value); 
                    ret.Path = site.Path;
                    ret.SiteId = args.ValidationSiteId.Value;
                }
            }
            return ret;
        }

        public override FileSystemOptions Aquire(Target target, IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            // Choose alternative site for validation
            var args = optionsService.GetArguments<FileSystemArguments>();
            var ret = new FileSystemOptions(BaseAquire(target, args, optionsService, inputService, runLevel));
            if (target.IIS && _iisClient.HasWebSites && string.IsNullOrEmpty(ret.Path))
            {
                if (inputService.PromptYesNo("Use different site for validation?"))
                {
                    var site = inputService.ChooseFromList("Validation site, must receive requests for all hosts on port 80",
                        _iisClient.WebSites,
                        x => new Choice<IIISSite>(x) { Command = x.Id.ToString(), Description = x.Name }, true);
                    if (site != null)
                    {
                        ret.Path = site.Path;
                        ret.SiteId = site.Id;
                    }
                }
            }
            return ret;
        }
    }

}
