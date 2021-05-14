using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Services;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.ValidationPlugins.Http
{
    /// <summary>
    /// Classic FileSystem validation
    /// </summary>
    internal class FileSystemOptionsFactory : HttpValidationOptionsFactory<FileSystem, FileSystemOptions>
    {
        private readonly IIISClient _iisClient;
        private readonly ILogService _log;

        public FileSystemOptionsFactory(
            IIISClient iisClient, ILogService log,
            ArgumentsInputService arguments) : base(arguments)
        {
            _log = log;
            _iisClient = iisClient;
        }

        public override bool PathIsValid(string path) => path.ValidPath(_log);
        public override bool AllowEmtpy(Target target) => target.IIS;
        private ArgumentResult<long?> ValidationSite() => _arguments.GetLong<FileSystemArguments>(x => x.ValidationSiteId);

        public override async Task<FileSystemOptions?> Default(Target target)
        {
            var ret = new FileSystemOptions(await BaseDefault(target));
            if (string.IsNullOrEmpty(ret.Path))
            {
                if (target.IIS && _iisClient.HasWebSites)
                {
                    ret.SiteId = await ValidationSite().
                        Validate(s => Task.FromResult(_iisClient.GetWebSite(s!.Value) != null), "site doesn't exist").
                        GetValue();
                }
            }
            return ret;
        }

        public override async Task<FileSystemOptions?> Aquire(Target target, IInputService inputService, RunLevel runLevel)
        {
            // Choose alternative site for validation
            var ret = new FileSystemOptions(await BaseAquire(target, inputService));
            if (target.IIS && 
                _iisClient.HasWebSites &&
                string.IsNullOrEmpty(ret.Path) && 
                runLevel.HasFlag(RunLevel.Advanced))
            {
                var siteId = await ValidationSite().GetValue();
                if (siteId != null || await inputService.PromptYesNo("Use different site for validation?", false))
                {
                    var site = await inputService.ChooseOptional("Validation site, must receive requests for all hosts on port 80",
                        _iisClient.WebSites,
                        x => Choice.Create<IIISSite?>(x, x.Name, x.Id.ToString(), @default: x.Id == siteId),
                        "Automatic (target site)");
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
