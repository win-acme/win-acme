using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISSiteOptionsFactory : TargetPluginOptionsFactory<IISSite, IISSiteOptions>
    {
        public override bool Hidden => !_iisClient.HasWebSites;
        protected IIISClient _iisClient;
        protected IISSiteHelper _siteHelper;
        protected IISSiteOptionsHelper _optionsHelper;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public IISSiteOptionsFactory(
            ILogService log, IIISClient iisClient,
            IISSiteHelper helper, IArgumentsService arguments)
        {
            _iisClient = iisClient;
            _siteHelper = helper;
            _optionsHelper = new IISSiteOptionsHelper(log);
            _log = log;
            _arguments = arguments;
        }

        public async override Task<IISSiteOptions> Aquire(IInputService input, RunLevel runLevel)
        {
            var ret = new IISSiteOptions();
            var sites = _siteHelper.
                GetSites(_arguments.MainArguments.HideHttps, true).
                Where(x => x.Hidden == false).
                Where(x => x.Hosts.Any());
            if (!sites.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }
            var chosen = input.ChooseFromList("Choose site",
                sites,
                x => Choice.Create(x, x.Name),
                "Abort");
            if (chosen != null)
            {
                ret.SiteId = chosen.Id;
                if (_optionsHelper.AquireAdvancedOptions(input, chosen.Hosts, runLevel, ret))
                {
                    return ret;
                }
                else
                {
                    return null;
                }
            }
            return null;
        }

        public async override Task<IISSiteOptions> Default()
        {
            var ret = new IISSiteOptions();
            var args = _arguments.GetArguments<IISSiteArguments>();
            var rawSiteId = _arguments.TryGetRequiredArgument(nameof(args.SiteId), args.SiteId);
            if (long.TryParse(rawSiteId, out var siteId))
            {
                var site = _siteHelper.GetSites(false, false).FirstOrDefault(binding => binding.Id == siteId);
                if (site != null)
                {
                    ret.SiteId = site.Id;
                    _optionsHelper.DefaultAdvancedOptions(args, site.Hosts, ret);
                    return ret;
                }
                else
                {
                    _log.Error("Unable to find SiteId {siteId}", siteId);
                }
            }
            else
            {
                _log.Error("Invalid SiteId {siteId}", args.SiteId);
            }
            return null;
        }
    }
}
