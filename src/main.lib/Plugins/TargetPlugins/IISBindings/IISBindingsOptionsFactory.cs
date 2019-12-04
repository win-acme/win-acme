using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class IISBindingsOptionsFactory : TargetPluginOptionsFactory<IISBindings, IISBindingsOptions>
    {
        private readonly IISBindingHelper _bindingHelper;
        private readonly IISSiteHelper _siteHelper;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public IISBindingsOptionsFactory(
            ILogService log,
            IIISClient iisClient,
            IISBindingHelper bindingHelper,
            IISSiteHelper siteHelper,
            IArgumentsService arguments,
            UserRoleService userRoleService)
        {
            _bindingHelper = bindingHelper;
            _siteHelper = siteHelper;
            _log = log;
            _arguments = arguments;
            Hidden = !(iisClient.Version.Major > 6);
            Disabled = IISBindings.Disabled(userRoleService);
        }

        public override int Order => 2;

        public override async Task<IISBindingsOptions?> Aquire(IInputService input, RunLevel runLevel)
        {
            var ret = new IISBindingsOptions();
            return ret;
            //var bindings = _helper.GetBindings().Where(x => !_arguments.MainArguments.HideHttps || x.Https == false);

            //if (!bindings.Any())
            //{
            //    _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
            //    return null;
            //}

            //var chosenTarget = await input.ChooseFromList(
            //    "Choose selection mode",
            //    new[] {
            //        Choice.Create(IISBindingsSearchMode.Csv, "Enter host names separated by commas"),
            //        Choice.Create(IISBindingsSearchMode.Pattern, "Enter a search string using * and ? as placeholders"),
            //        Choice.Create(IISBindingsSearchMode.Regex, "Enter a regular expression"),
            //    },
            //    x => x,
            //    "Abort");

            //Regex regEx;
            //string search;
            //await input.WritePagedList(bindings.Select(x =>
            //    Choice.Create(
            //        x,
            //        command: "",
            //        color: x.Https ? ConsoleColor.Gray : (ConsoleColor?)null)));

            //switch (chosenTarget)
            //{
            //    case IISBindingsSearchMode.Csv:
            //        {

            //            do
            //            {
            //                search = await input.RequestString("Enter a comma seperated string of host names");
            //                if (search != null)
            //                {
            //                    regEx = TryParseRegEx(HostsToRegex(search.ParseCsv()));
            //                }
            //            } while (!await ListMatchingBindings(bindings, regEx, input));
            //            return new IISBindingsOptions { IncludeHosts = search };
            //        }

            //    case IISBindingsSearchMode.Pattern:
            //        {
            //            do
            //            {
            //                search = await input.RequestString("Enter a search string using * and ? as placeholders");
            //                regEx = TryParseRegEx(PatternToRegex(search));
            //            } while (!await ListMatchingBindings(bindings, regEx, input));
            //            return new IISBindingsOptions { IncludePattern = search };
            //        }

            //    case IISBindingsSearchMode.Regex:
            //        {
            //            do
            //            {
            //                search = await input.RequestString("Enter a regular expression");
            //                regEx = TryParseRegEx(search);
            //            } while (!await ListMatchingBindings(bindings, regEx, input));
            //            return new IISBindingsOptions { IncludeRegex = regEx };
            //        }

            //    default:
            //        return null;
            //}
        }

        //private async Task<bool> ListMatchingBindings(IEnumerable<IISBindingHelper.IISBindingOption> bindings, Regex regEx, IInputService input)
        //{
        //    if (regEx == null)
        //    {
        //        return false;
        //    }
        //    var matches = bindings.Where(binding => help.Matches(binding, regEx));
        //    if (matches.Any())
        //    {
        //        await input.WritePagedList(matches.Select(x =>
        //            Choice.Create(
        //                x,
        //                command: "",
        //                color: x.Https ? ConsoleColor.Gray : (ConsoleColor?)null)));
        //    }
        //    else
        //    {
        //        input.Show(null, "No matching hosts found.");
        //    }

        //    return await input.PromptYesNo("Should the search pattern be used?", matches.Any());
        //}

        private Regex? TryParseRegEx(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return default;
            }
            try
            {
                return new Regex(pattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException($"Invalid regular expression: {pattern}");
            }
        }

        public override async Task<IISBindingsOptions?> Default()
        {
            var options = new IISBindingsOptions();
            var args = _arguments.GetArguments<IISBindingsArguments>();

            options.IncludeHosts = args.Host.ParseCsv();
            try
            {
                options.IncludeSiteIds = ParseSiteIds(args.SiteId);
            } 
            catch
            {
                return null;
            }

            var filterSet = _bindingHelper.FilterBindings(options);
            if (!filterSet.Any())
            {
                _log.Error("No matching hosts found with selected filters");
                return null;
            }

            return options;
        }

        private List<long>? ParseSiteIds(string? sanInput)
        {
            if (string.IsNullOrEmpty(sanInput))
            {
                return null;
            }
            if (string.Equals(sanInput, "s", StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            var identifiers = sanInput.ParseCsv();
            if (identifiers == null)
            {
                throw new InvalidOperationException();
            }

            var ret = new List<long>();
            var siteList = _siteHelper.GetSites(false);
            foreach (var identifierString in identifiers)
            {
                if (long.TryParse(identifierString, out var id))
                {
                    var site = siteList.Where(t => t.Id == id).FirstOrDefault();
                    if (site != null)
                    {
                        ret.Add(site.Id);
                    }
                    else
                    {
                        _log.Error($"SiteId '{id}' not found");
                        throw new ArgumentException();
                    }
                }
                else
                {
                    _log.Error($"Invalid SiteId '{id}', should be a number");
                    throw new ArgumentException();
                }
            }
            return ret;
        }
    }
}