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
        private readonly IISHelper _iisHelper;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public IISBindingsOptionsFactory(
            ILogService log,
            IIISClient iisClient,
            IISHelper iisHelper,
            IArgumentsService arguments,
            UserRoleService userRoleService)
        {
            _iisHelper = iisHelper;
            _log = log;
            _arguments = arguments;
            Hidden = !(iisClient.Version.Major > 6);
            Disabled = IISBindings.Disabled(userRoleService);
        }

        public override int Order => 2;

        public override bool Match(string name)
        {
            switch (name.ToLowerInvariant())
            {
                case "iisbinding":
                case "iissite":
                case "iissites":
                    return true;
                default:
                    return base.Match(name);
            }
        }

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
            if (args == null)
            {
                return null;
            }
            if (string.IsNullOrWhiteSpace(args.Host) && 
                string.IsNullOrWhiteSpace(args.SiteId))
            {
                // Logically this would be a no-filter run: all 
                // bindings for all websites. Because the impact
                // of that can be so high, we want the user to
                // be explicit about it.
                _log.Error("You have not specified any filters. If you are sure that you want " +
                    "to create a certificate for *all* bindings on the server, please specific " +
                    "-siteid s");
                return null;
            }

            if (!DefaultHostOptions(args, options))
            {
                return null;
            }
            if (!DefaultSiteOptions(args, options))
            {
                return null;
            }
          
            var filterSet = _iisHelper.FilterBindings(options);
            if (!filterSet.Any())
            {
                _log.Error("No matching hosts found with selected filters");
                return null;
            }

            if (!DefaultCommonName(args, filterSet.Select(x => x.HostUnicode), options))
            {
                return null;
            }

            return options;
        }

        /// <summary>
        /// Host filtering options in unattended mode
        /// </summary>
        /// <param name="args"></param>
        /// <param name="bindings"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private bool DefaultHostOptions(IISBindingsArguments args, IISBindingsOptions ret)
        {
            var specifiedHosts = args.Host.ParseCsv();
            if (specifiedHosts != null)
            {
                var bindings = _iisHelper.GetBindings();
                foreach (var specifiedHost in specifiedHosts)
                {
                    var binding = bindings.FirstOrDefault(
                        x => x.HostUnicode == specifiedHost ||
                        x.HostPunycode == specifiedHost);
                    if (binding != null)
                    {
                        if (ret.IncludeHosts == null)
                        {
                            ret.IncludeHosts = new List<string>();
                        }
                        ret.IncludeHosts.Add(binding.HostUnicode);
                    }
                    else
                    {
                        _log.Error("Unable to find binding {specifiedHost}", specifiedHost);
                        return false;
                    }
                }
            }

            ret.ExcludeHosts = args.ExcludeBindings.ParseCsv();
            if (ret.ExcludeHosts != null)
            {
                ret.ExcludeHosts = ret.ExcludeHosts.Select(x => x.ConvertPunycode()).ToList();
            }

            return true;
        }

        /// <summary>
        /// Advanced options in unattended mode
        /// </summary>
        /// <param name="args"></param>
        /// <param name="chosen"></param>
        /// <param name="ret"></param>
        /// <returns></returns>
        private bool DefaultCommonName(IISBindingsArguments args, IEnumerable<string> chosen, IISBindingsOptions ret)
        {
            var commonName = args.CommonName;
            if (!string.IsNullOrWhiteSpace(commonName))
            {
                commonName = commonName.ToLower().Trim().ConvertPunycode();
                if (chosen.Contains(commonName))
                {
                    ret.CommonName = commonName;
                }
                else
                {
                    _log.Error("Common name {commonName} not found or excluded", commonName);
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// SiteId filter in unattended mode
        /// </summary>
        /// <param name="args"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool DefaultSiteOptions(IISBindingsArguments args, IISBindingsOptions options)
        {
            if (string.IsNullOrEmpty(args.SiteId))
            {
                return true;
            }
            if (string.Equals(args.SiteId, "s", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            var identifiers = args.SiteId.ParseCsv();
            if (identifiers == null)
            {
                throw new InvalidOperationException();
            }

            var ret = new List<long>();
            var siteList = _iisHelper.GetSites(false);
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
                        return false;
                    }
                }
                else
                {
                    _log.Error($"Invalid SiteId '{id}', should be a number");
                    return false;
                }
            }
            options.IncludeSiteIds = ret;
            return true;
        }
    }
}