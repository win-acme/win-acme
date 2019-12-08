//using PKISharp.WACS.Extensions;
//using PKISharp.WACS.Services;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace PKISharp.WACS.Plugins.TargetPlugins
//{
//    internal class IISSiteOptionsHelper
//    {
//        public async Task<bool> AquireAdvancedOptions(IInputService input, IEnumerable<string> chosen, RunLevel runLevel, IIISSiteOptions ret)
//        {
//            if (runLevel.HasFlag(RunLevel.Advanced))
//            {
//                await input.WritePagedList(chosen.Select(x => Choice.Create(x, "")));
//                // Exclude bindings 
//                if (chosen.Count() > 1)
//                {
//                    ret.ExcludeBindings = (await input.RequestString("Press enter to include all listed hosts, or type a comma-separated lists of exclusions")).ParseCsv();
//                }
//            }

//            var remaining = chosen.Except(ret.ExcludeBindings ?? new List<string>());
//            if (remaining.Count() == 0)
//            {
//                _log.Error("No bindings remain");
//                return false;
//            }

//            // Set common name
//            if (remaining.Count() > 1)
//            {
//                ret.CommonName = await input.ChooseFromList(
//                    "Select primary domain (common name)",
//                    remaining,
//                    x => Choice.Create(x),
//                    "Default");
//            }
//            return true;
//        }
//    }