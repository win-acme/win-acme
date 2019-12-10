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

        /// <summary>
        /// Match with the legacy target plugin names
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Get settings in interactive mode
        /// </summary>
        /// <param name="input"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
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

            // Repeat the process until the user is happy with their settings
            do
            {
                var allBindings = _iisHelper.GetBindings();
                var visibleBindings = allBindings.Where(x => !_arguments.MainArguments.HideHttps || x.Https == false).ToList();
                var ret = await TryAquireSettings(input, allBindings, visibleBindings, allSites, visibleSites, runLevel);
                if (ret != null)
                {
                    var filtered = _iisHelper.FilterBindings(allBindings, ret);
                    await ListBindings(input, filtered, false);
                    if (await input.PromptYesNo("Use this filter?", true))
                    {
                        return ret;
                    }
                }
                if (!await input.PromptYesNo("Start again?", true))
                {
                    return null;
                }
            } 
            while (true);
        }

        /// <summary>
        /// Single round of aquiring settings
        /// </summary>
        /// <param name="input"></param>
        /// <param name="allBindings"></param>
        /// <param name="visibleBindings"></param>
        /// <param name="allSites"></param>
        /// <param name="visibleSites"></param>
        /// <param name="runLevel"></param>
        /// <returns></returns>
        private async Task<IISBindingsOptions?> TryAquireSettings(
            IInputService input, 
            List<IISHelper.IISBindingOption> allBindings,
            List<IISHelper.IISBindingOption> visibleBindings,
            List<IISHelper.IISSiteOption> allSites,
            List<IISHelper.IISSiteOption> visibleSites,
            RunLevel runLevel)
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

            var askExclude = true;
            var filtered = _iisHelper.FilterBindings(visibleBindings, options);
            await ListBindings(input, filtered, true);
            input.Show(null, "You may either choose to include all listed bindings, or apply an additional filter", true);
            var filters = new List<Choice<Func<Task>>>
            {
                Choice.Create<Func<Task>>(() => {
                    askExclude = false;
                    return InputHosts(
                        "Include bindings", input, allBindings, filtered, options,
                        () => options.IncludeHosts, x => options.IncludeHosts = x);
                }, "Pick specific bindings from a list"),
                Choice.Create<Func<Task>>(() => {
                    return InputPattern(input, options); 
                }, "Use simple pattern matching with * and ?"),
                Choice.Create<Func<Task>>(() => { 
                    askExclude = false; 
                    return Task.CompletedTask; 
                }, "None", @default: true)
            };
            if (runLevel.HasFlag(RunLevel.Advanced))
            {
                filters.Insert(2, Choice.Create<Func<Task>>(() => { 
                    askExclude = true; 
                    return InputRegex(input, options);
                }, "Use a regular expression"));
            }
            var chosen = await input.ChooseFromList("Binding filter", filters);
            await chosen.Invoke();

            // Exclude specific bindings
            if (askExclude && runLevel.HasFlag(RunLevel.Advanced))
            {
                filtered = _iisHelper.FilterBindings(allBindings, options);
                await ListBindings(input, filtered, true);
                input.Show(null, "The following bindings match your current filter settings. " +
                    "If you wish to exclude one or more of them from the certificate, please " +
                    "input those bindings now. Press <ENTER> to include all listed bindings.", true);
                await InputHosts("Exclude bindings", 
                    input, allBindings, filtered, options, 
                    () => options.ExcludeHosts, x => options.ExcludeHosts = x);
            }

            // Now the common name
            if (options.ExcludeHosts != null)
            {
                filtered = _iisHelper.FilterBindings(allBindings, options);
                await ListBindings(input, filtered, true);
            }
            if (filtered.Count > 1)
            {
                await InputCommonName(input, filtered, options);
            }
            return options;
        }

        /// <summary>
        /// Allows users to input both the full host name, or the number that
        /// it's referred to by in the displayed list
        /// </summary>
        /// <param name="label"></param>
        /// <param name="input"></param>
        /// <param name="allBindings"></param>
        /// <param name="filtered"></param>
        /// <param name="options"></param>
        /// <param name="get"></param>
        /// <param name="set"></param>
        /// <returns></returns>
        async Task InputHosts(
            string label,
            IInputService input,
            List<IISHelper.IISBindingOption> allBindings,
            List<IISHelper.IISBindingOption> filtered, 
            IISBindingsOptions options,
            Func<List<string>?> get,
            Action<List<string>> set)
        {
            var raw = default(string);
            do
            {
                raw = await input.RequestString(label);
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
            while (!ParseHostOptions(raw, allBindings, options, get, set));
        }

        async Task InputCommonName(IInputService input, List<IISHelper.IISBindingOption> filtered, IISBindingsOptions options)
        {
            string raw;
            do
            {
                raw = await input.RequestString("Common name");
                if (!string.IsNullOrEmpty(raw))
                {
                    // Magically replace binding identifiers by their proper host names
                    if (int.TryParse(raw, out var id))
                    {
                        if (id > 0 && id <= filtered.Count())
                        {
                            raw = filtered[id - 1].HostUnicode;
                        }
                    }
                }
            }
            while (!ParseCommonName(raw, filtered.Select(x => x.HostUnicode), options));
        }

        /// <summary>
        /// Interactive input of a search pattern
        /// </summary>
        /// <param name="input"></param>
        /// <param name="options"></param>
        /// <returns></returns>
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
            var sortedBindings = bindings.OrderBy(x => x.HostUnicode).ThenBy(x => x.SiteId);
            await input.WritePagedList(
               sortedBindings.Select(x => Choice.Create(
                   item: x,
                   command: number ? null : "*",
                   color: x.Https ? ConsoleColor.DarkGray : (ConsoleColor?)null)));
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
            if (!ParseHostOptions(args.Host, allBindings, options, () => options.IncludeHosts, x => options.IncludeHosts = x))
            {
                return null;
            }
            if (options.IncludeHosts != null && !string.IsNullOrWhiteSpace(args.Pattern))
            {
                _log.Error("Parameters --host and --hosts-pattern cannot be combined");
                return null;
            }
            if (options.IncludeHosts == null)
            {
                if (!ParseHostOptions(args.ExcludeBindings, allBindings, options, () => options.ExcludeHosts, x => options.ExcludeHosts = x))
                {
                    return null;
                }
            }
            if (args.Pattern != null && !ParsePattern(args.Pattern, options))
            {
                return null;
            }
            if (options.IncludePattern != null && !string.IsNullOrWhiteSpace(args.Regex))
            {
                _log.Error("Parameters --host-pattern and --hosts-regex cannot be combined");
                return null;
            }
            if (args.Regex != null && !ParseRegex(args.Regex, options))
            {
                return null;
            }

            var filterSet = _iisHelper.FilterBindings(allBindings, options);
            if (!filterSet.Any())
            {
                _log.Error("No bindings found within selected filters");
                return null;
            }

            if (args.CommonName != null)
            {
                if (!ParseCommonName(args.CommonName, filterSet.Select(x => x.HostUnicode), options))
                {
                    return null;
                }
            }

            return options;
        }

        /// <summary>
        /// Host filtering options in unattended mode
        /// </summary>
        /// <param name="args"></param>
        /// <param name="bindings"></param>
        /// <param name="options"></param>
        private bool ParseHostOptions(
            string? input,
            List<IISHelper.IISBindingOption> allBindings, 
            IISBindingsOptions options,
            Func<List<string>?> get,
            Action<List<string>> set)
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
                        var list = get();
                        if (list == null)
                        {
                            list = new List<string>();
                            set(list);
                        }
                        list.Add(filteredBinding.HostUnicode);
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
        private bool ParseCommonName(string commonName, IEnumerable<string> chosen, IISBindingsOptions ret)
        {
            if (string.IsNullOrWhiteSpace(commonName))
            {
                return false;
            }
            commonName = commonName.ToLower().Trim().ConvertPunycode();
            if (chosen.Contains(commonName))
            {
                ret.CommonName = commonName;
                return true;
            }
            else
            {
                _log.Error("Common name {commonName} not found or excluded", commonName);
                return false;
            }
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