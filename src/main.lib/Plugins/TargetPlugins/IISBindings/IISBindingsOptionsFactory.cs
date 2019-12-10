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
            var allSites = _iisHelper.GetSites(true).Where(x => x.Hosts.Any()).ToList();
            if (!allSites.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. " +
                    $"Add one in the IIS Manager or choose the plugin '{ManualOptions.DescriptionText}' " +
                    $"instead.");
                return null;
            }

            var visibleSites = allSites.Where(x => !_arguments.MainArguments.HideHttps || x.Https == false).ToList();
            if (!visibleSites.Any())
            {
                _log.Error("No sites with named bindings remain after applying the --{hidehttps} filter. " +
                    "It looks like all your websites are already configured for https!", "hidehttps");
                return null;
            }

            // Scan all bindings
            do
            {
                var allBindings = _iisHelper.GetBindings();
                var visibleBindings = allBindings.Where(x => !_arguments.MainArguments.HideHttps || x.Https == false).ToList();
                var ret = await TryAquireSettings(input, allBindings, visibleBindings, allSites, visibleSites);
                if (ret != null)
                {
                    var filtered = _iisHelper.FilterBindings(allBindings, ret);
                    await ListBindings(input, filtered, false);
                    if (await input.PromptYesNo("Accept this filter?", true))
                    {
                        return ret;
                    }
                }
                if (!await input.PromptYesNo("Try again?", true))
                {
                    return null;
                }
            } 
            while (true);
        }

        private async Task<IISBindingsOptions?> TryAquireSettings(
            IInputService input, 
            List<IISHelper.IISBindingOption> allBindings,
            List<IISHelper.IISBindingOption> visibleBindings,
            List<IISHelper.IISSiteOption> allSites,
            List<IISHelper.IISSiteOption> visibleSites)
        {
            input.Show(null, "Please select which website(s) should be scanned for host names. " +
                "You may input one or more site identifiers (comma separated) to filter by those sites, " +
                "or alternatively leave the input empty to scan *all* websites.", true);

            var options = new IISBindingsOptions();
            await input.WritePagedList(
                visibleSites.Select(x => Choice.Create(
                    item: x,
                    description: $"{x.Name} ({x.Hosts.Count()} binding{(x.Hosts.Count() == 1 ? "" : "s")})",
                    command: x.Id.ToString(),
                    color: x.Https ? ConsoleColor.DarkGray : (ConsoleColor?)null)));
            var raw = await input.RequestString("Site identifier(s) or <ENTER> to choose all");
            if (!ParseSiteOptions(raw, allSites, options))
            {
                return null;
            }

            var filtered = _iisHelper.FilterBindings(visibleBindings, options);
            await ListBindings(input, filtered, true);
            input.Show(null, "You may either choose to include all host names or apply an additional filter", true);
            var filters = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(() => InputHosts(input, allBindings, filtered, options), "Pick specific bindings from a list"),
                Choice.Create<Func<Task>>(() => InputPattern(input, options), "Use simple pattern matching with * and ?"),
                Choice.Create<Func<Task>>(() => InputRegex(input, options), "Use a regular expression"),
                Choice.Create<Func<Task>>(() => Task.CompletedTask, "None", @default: true)
            };
            var chosen = await input.ChooseFromList("Filter", filters);
            await chosen.Invoke();

            // Now the common options

            return options;
        }

        async Task InputHosts(
            IInputService input,
            List<IISHelper.IISBindingOption> allBindings,
            List<IISHelper.IISBindingOption> filtered, 
            IISBindingsOptions options)
        {
            var raw = default(string);
            do
            {
                raw = await input.RequestString("List of host names to include");
                if (!string.IsNullOrEmpty(raw))
                {
                    // Magically replace binding identifiers by their proper host names
                    raw = string.Join(",", raw.ParseCsv().Select(x =>
                    {
                        if (int.TryParse(x, out var id))
                        {
                            if (id > 0 && id <= filtered.Count())
                            {
                                return filtered[id - 1].HostUnicode;
                            }
                        }
                        return x;
                    }));
                }
            }
            while (!ParseHostOptions(raw, allBindings, options));
        }

        async Task InputPattern(IInputService input, IISBindingsOptions options)
        {
            input.Show(null, IISBindingsArgumentsProvider.PatternExamples, true);
            string raw;
            do
            {
                raw = await input.RequestString("Pattern");
            }
            while (!ParsePattern(raw, options));
        }

        async Task InputRegex(IInputService input, IISBindingsOptions options)
        {
            string raw;
            do
            {
                raw = await input.RequestString("Regex");
            }
            while (!ParsePattern(raw, options));
        }

        private bool ParsePattern(string? pattern, IISBindingsOptions ret)
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                _log.Error("Invalid input");
                return false;
            }
            try
            {
                var regexString = _iisHelper.PatternToRegex(pattern);
                var actualRegex = new Regex(regexString);
                ret.IncludePattern = pattern;
                return true;
            } 
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to convert pattern to regex");
                return false;
            }
        }


        private bool ParseRegex(string? regex, IISBindingsOptions options)
        {
            if (string.IsNullOrWhiteSpace(regex))
            {
                _log.Error("Invalid input");
                return false;
            }
            try
            {
                var regexString = _iisHelper.PatternToRegex(regex);
                var actualRegex = new Regex(regexString);
                options.IncludeRegex = actualRegex;
                return true;
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to convert pattern to regex");
                return false;
            }
        }


        private async Task ListBindings(IInputService input, List<IISHelper.IISBindingOption> bindings, bool number)
        {
            await input.WritePagedList(
               bindings.Select(x => Choice.Create(
                   item: x,
                   command: number ? null : "*",
                   color: x.Https ? ConsoleColor.DarkGray : (ConsoleColor?)null)));
        }

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

            var allSites = _iisHelper.GetSites(false);
            if (!ParseSiteOptions(args.SiteId, allSites, options))
            {
                return null;
            }

            var allBindings = _iisHelper.GetBindings();
            if (!DefaultExcludeOptions(args, allBindings, options))
            {
                return null;
            }


            if (!ParseHostOptions(args.Host, allBindings, options))
            {
                return null;
            }
            if (options.IncludeHosts != null && !string.IsNullOrWhiteSpace(args.Pattern))
            {
                _log.Error("Parameters --host and --hosts-pattern cannot be combined");
                return null;
            }
            if (!ParsePattern(args.Pattern, options))
            {
                return null;
            }
            if (options.IncludePattern != null && !string.IsNullOrWhiteSpace(args.Regex))
            {
                _log.Error("Parameters --host-pattern and --hosts-regex cannot be combined");
                return null;
            }
            if (!ParseRegex(args.Regex, options))
            {
                return null;
            }

            var filterSet = _iisHelper.FilterBindings(allBindings, options);
            if (!filterSet.Any())
            {
                _log.Error("No bindings found within selected filters");
                return null;
            }

            if (!DefaultCommonName(args, filterSet.Select(x => x.HostUnicode), options))
            {
                return null;
            }

            return options;
        }

        private bool DefaultExcludeOptions(IISBindingsArguments args, List<IISHelper.IISBindingOption> allBindings, IISBindingsOptions ret)
        {
            // First process excludes
            ret.ExcludeHosts = args.ExcludeBindings.ParseCsv();
            if (ret.ExcludeHosts != null)
            {
                ret.ExcludeHosts = ret.ExcludeHosts.Select(x => x.ConvertPunycode()).ToList();
            }
            return true;
        }

        /// <summary>
        /// Host filtering options in unattended mode
        /// </summary>
        /// <param name="args"></param>
        /// <param name="bindings"></param>
        /// <param name="options"></param>
        /// <returns></returns>
        private bool ParseHostOptions(string? input, List<IISHelper.IISBindingOption> allBindings, IISBindingsOptions options)
        {
            var specifiedHosts = input.ParseCsv();
            if (specifiedHosts != null)
            {
                var filteredBindings = _iisHelper.FilterBindings(allBindings, options);
                foreach (var specifiedHost in specifiedHosts)
                {
                    var filteredBinding = filteredBindings.FirstOrDefault(x => x.HostUnicode == specifiedHost || x.HostPunycode == specifiedHost);
                    var binding = allBindings.FirstOrDefault(x => x.HostUnicode == specifiedHost || x.HostPunycode == specifiedHost);
                    if (filteredBinding != null)
                    {
                        if (options.IncludeHosts == null)
                        {
                            options.IncludeHosts = new List<string>();
                        }
                        options.IncludeHosts.Add(filteredBinding.HostUnicode);
                    }
                    else if (binding != null)
                    {
                        _log.Error("Binding {specifiedHost} is excluded by another filter", specifiedHost);
                        return false;
                    }
                    else
                    {
                        _log.Error("Binding {specifiedHost} not found", specifiedHost);
                        return false;
                    }
                }
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
        private bool ParseSiteOptions(string? input, List<IISHelper.IISSiteOption> sites, IISBindingsOptions options)
        {
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }
            if (string.Equals(input, "s", StringComparison.CurrentCultureIgnoreCase))
            {
                return true;
            }

            var identifiers = input.ParseCsv();
            if (identifiers == null)
            {
                throw new InvalidOperationException();
            }

            var ret = new List<long>();
            foreach (var identifierString in identifiers)
            {
                if (long.TryParse(identifierString, out var id))
                {
                    var site = sites.Where(t => t.Id == id).FirstOrDefault();
                    if (site != null)
                    {
                        ret.Add(site.Id);
                    }
                    else
                    {
                        _log.Error("Site identifier '{id}' not found", id);
                        return false;
                    }
                }
                else
                {
                    _log.Error("Invalid site identifier '{identifierString}', should be a number", identifierString);
                    return false;
                }
            }
            options.IncludeSiteIds = ret;
            return true;
        }
    }
}