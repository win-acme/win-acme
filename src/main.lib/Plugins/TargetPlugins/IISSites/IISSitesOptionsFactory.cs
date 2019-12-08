//using PKISharp.WACS.Clients.IIS;
//using PKISharp.WACS.Extensions;
//using PKISharp.WACS.Plugins.Base.Factories;
//using PKISharp.WACS.Services;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace PKISharp.WACS.Plugins.TargetPlugins
//{
//    internal class IISSitesOptionsFactory : TargetPluginOptionsFactory<IISSites, IISSitesOptions>
//    {
//        public override async Task<IISSitesOptions> Aquire(IInputService input, RunLevel runLevel)
//        {
//            var ret = new IISSitesOptions();
//            var sites = _siteHelper.GetSites(true).
//                Where(x => !_arguments.MainArguments.HideHttps || x.Https == false).
//                Where(x => x.Hosts.Any()).
//                ToList();
//            if (!sites.Any())
//            {
//                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
//                return null;
//            }
//            await input.WritePagedList(
//                sites.Select(x => 
//                    Choice.Create(x, 
//                        $"{x.Name} ({x.Hosts.Count()} binding{(x.Hosts.Count()==1?"":"s")})", 
//                        x.Id.ToString(),
//                        color: x.Https ? ConsoleColor.DarkGray : (ConsoleColor?)null)).ToList());

//            var sanInput = await input.RequestString("Enter a comma separated list of SiteIds or 'S' for all sites");
//            sites = ProcessSiteIds(ret, sites, sanInput);
//            if (sites != null)
//            {
//                var hosts = sites.SelectMany(x => x.Hosts).Distinct().OrderBy(x => x);
//                if (await _optionsHelper.AquireAdvancedOptions(input, hosts, runLevel, ret))
//                {
//                    return ret;
//                }
//            }
//            return null;
//        }
//    }
//}
