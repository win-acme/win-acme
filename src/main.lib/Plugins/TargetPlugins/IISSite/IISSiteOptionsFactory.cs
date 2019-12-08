//using PKISharp.WACS.Clients.IIS;
//using PKISharp.WACS.Plugins.Base.Factories;
//using PKISharp.WACS.Services;
//using System;
//using System.Linq;
//using System.Threading.Tasks;

//namespace PKISharp.WACS.Plugins.TargetPlugins
//{
//    internal class IISSiteOptionsFactory : TargetPluginOptionsFactory<IISBindings, IISSiteOptions>
//    {
//        public async override Task<IISSiteOptions> Aquire(IInputService input, RunLevel runLevel)
//        {
//            var ret = new IISSiteOptions();
//            var sites = _siteHelper.
//                GetSites(true).
//                Where(x => !_arguments.MainArguments.HideHttps || x.Https == false).
//                Where(x => x.Hosts.Any());

//            if (!sites.Any())
//            {
//                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
//                return null;
//            }

//            var chosen = await input.ChooseFromList("Choose site",
//                sites,
//                x => Choice.Create(x,
//                        $"{x.Name} ({x.Hosts.Count()} binding{(x.Hosts.Count() == 1 ? "" : "s")})",
//                        x.Id.ToString(),
//                        color: x.Https ? ConsoleColor.DarkGray : (ConsoleColor?)null),
//                "Abort");
//            if (chosen != null)
//            {
//                ret.SiteId = chosen.Id;
//                if (await _optionsHelper.AquireAdvancedOptions(input, chosen.Hosts, runLevel, ret))
//                {
//                    return ret;
//                }
//            }
//            return null;
//        }
//}
