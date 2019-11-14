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
    [Flags]
    internal enum IISBindingsSearchMode
    {
        Unknown = 0,
        Pattern = 1,
        Regex = 2,
        Csv = 4 
    }

    internal class IISBindingsOptionsFactory : TargetPluginOptionsFactory<IISBindings, IISBindingsOptions>
    {
        private readonly IISBindingHelper _helper;
        private readonly ILogService _log;
        private readonly IArgumentsService _arguments;

        public IISBindingsOptionsFactory(
            ILogService log, IIISClient iisClient,
            IISBindingHelper helper, IArgumentsService arguments,
            UserRoleService userRoleService)
        {
            _helper = helper;
            _log = log;
            _arguments = arguments;
            Hidden = !(iisClient.Version.Major > 6);
            Disabled = IISBindings.Disabled(userRoleService);
        }

        public override int Order => 2;

        public override async Task<IISBindingsOptions> Aquire(IInputService input, RunLevel runLevel)
        {
            var ret = new IISBindingsOptions();
            var bindings = _helper.GetBindings().Where(x => !_arguments.MainArguments.HideHttps || x.Https == false);

            if (!bindings.Any())
            {
                _log.Error($"No sites with named bindings have been configured in IIS. Add one or choose '{ManualOptions.DescriptionText}'.");
                return null;
            }

            var chosenTarget = await input.ChooseFromList(
                "Choose selection mode",
                new[] {
                    Choice.Create(IISBindingsSearchMode.Csv, "Enter host names separated by commas"),
                    Choice.Create(IISBindingsSearchMode.Pattern, "Enter a search string using * and ? as placeholders"),
                    Choice.Create(IISBindingsSearchMode.Regex, "Enter a regular expression"),
                },
                x => x,
                "Abort");

            Regex regEx;
            string search;
            switch (chosenTarget)
            {
                case IISBindingsSearchMode.Csv:
                    {
                        do
                        {
                            search = await input.RequestString("Enter a comma seperated string of host names");
                            regEx = TryParseRegEx(HostsToRegex(search));
                        } while (!await ListMatchingBindings(bindings, regEx, input));
                        return new IISBindingsOptions { Hosts = search };
                    }

                case IISBindingsSearchMode.Pattern:
                    {
                        do
                        {
                            search = await input.RequestString("Enter a search string using * and ? as placeholders");
                            regEx = TryParseRegEx(PatternToRegex(search));
                        } while (!await ListMatchingBindings(bindings, regEx, input));
                        return new IISBindingsOptions { Pattern = search };
                    }

                case IISBindingsSearchMode.Regex:
                    {
                        do
                        {
                            search = await input.RequestString("Enter a regular expression");
                            regEx = TryParseRegEx(search);
                        } while (!await ListMatchingBindings(bindings, regEx, input));
                        return new IISBindingsOptions { Regex = regEx };
                    }

                default:
                    return null;
            }
        }

        private Task<bool> ListMatchingBindings(IEnumerable<IISBindingHelper.IISBindingOption> bindings, Regex regEx, IInputService input)
        {
            if (regEx == null)
            {
                return Task.FromResult(false);
            }

            var matches = bindings.Where(binding => regEx.IsMatch(binding.HostUnicode));

            if (matches.Any())
            {
                input.Show("Matching hosts");

                foreach (var match in matches)
                {
                    input.Show(null, match.HostUnicode);
                }
            }
            else
            {
                input.Show(null, "No matching hosts found.");
            }

            return input.PromptYesNo("Should the search pattern be used?", matches.Any());
        }

        private Regex TryParseRegEx(string pattern)
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

        internal static string PatternToRegex(string pattern) => 
            $"^{Regex.Escape(pattern).Replace(@"\*", ".*").Replace(@"\?", ".")}$";

        internal static string HostsToRegex(string pattern) => 
            $"^({string.Join('|', pattern.ParseCsv().Select(x => Regex.Escape(x)))})$";

        public override Task<IISBindingsOptions> Default()
        {
            var options = new IISBindingsOptions();
            var args = _arguments.GetArguments<IISBindingsArguments>();
            var filterSet = _helper.GetBindings();
            var regEx = default(Regex);
            var type = IISBindingsSearchMode.Unknown;
            var failed = Task.FromResult(default(IISBindingsOptions)); 

            if (!string.IsNullOrEmpty(args.Pattern))
            {
                regEx = TryParseRegEx(PatternToRegex(args.Pattern));
                type = IISBindingsSearchMode.Pattern;
                options.Pattern = args.Pattern;
            }

            if (!string.IsNullOrEmpty(args.Regex))
            {
                if (type == IISBindingsSearchMode.Unknown)
                {
                    regEx = TryParseRegEx(args.Regex);
                    if (regEx == null)
                    {
                        return failed;
                    }
                    type = IISBindingsSearchMode.Regex;
                    options.Regex = regEx;
                } 
                else 
                {
                    _log.Error("Only one type of filter can be used: --{a}, --{b} or --{c}", 
                        nameof(args.Pattern).ToLower(),
                        nameof(args.Regex).ToLower(),
                        nameof(args.Hosts).ToLower());
                    return failed;
                }
            }

            if (!string.IsNullOrEmpty(args.Hosts))
            {
                if (type == IISBindingsSearchMode.Unknown)
                {
                    regEx = TryParseRegEx(HostsToRegex(args.Hosts));
                    type = IISBindingsSearchMode.Csv;
                    options.Hosts = args.Hosts;
                }
                else
                {
                    _log.Error("Only one type of filter can be used: --{a}, --{b} or --{c}",
                        nameof(args.Pattern).ToLower(),
                        nameof(args.Regex).ToLower(),
                        nameof(args.Hosts).ToLower());
                    return failed;
                }
            }

            if (type == IISBindingsSearchMode.Unknown)
            {
                _log.Error("At least one type of filter must be used: --{a}, --{b} or --{c}",
                    nameof(args.Pattern).ToLower(),
                    nameof(args.Regex).ToLower(),
                    nameof(args.Hosts).ToLower());
                return failed;
            }

            if (!filterSet.Any(binding => IISBindings.Matches(binding, regEx)))
            {
                _log.Error("No matching hosts found with selected filter");
                return failed;
            }

            return Task.FromResult(options);
        }
    }
}