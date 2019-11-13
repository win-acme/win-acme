using PKISharp.WACS.Clients.IIS;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal enum IISBindingsSearchMode
    {
        Unknown,
        Simple,
        RegEx
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
                new[]
                {
                Choice.Create(IISBindingsSearchMode.Simple, "Enter a search string using * and ? as placeholders", "1"),
                Choice.Create(IISBindingsSearchMode.RegEx, "Enter a regular expression", "2"),
                },
                x => x,
                "Abort");

            switch (chosenTarget)
            {
                case IISBindingsSearchMode.Simple:
                    {
                        Regex regEx;
                        string search;

                        do
                        {
                            search = await input.RequestString("Enter a search string using * and ? as placeholders");
                            regEx = TryParseRegEx(WildcardToRegex(search));
                        } while (!await ListMatchingBindings(bindings, regEx, input));

                        return new IISBindingsOptions { Simple = search };
                    }

                case IISBindingsSearchMode.RegEx:
                    {
                        Regex regEx;

                        do
                        {
                            var regexInput = await input.RequestString("Enter a regular expression");
                            regEx = TryParseRegEx(regexInput);

                        } while (!await ListMatchingBindings(bindings, regEx, input));

                        return new IISBindingsOptions { RegEx = regEx };
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
                return default;

            try
            {
                return new Regex(pattern, RegexOptions.IgnoreCase);
            }
            catch (ArgumentException)
            {
                _log.Error("Invalid regular expression", pattern);
            }

            return default;
        }

        internal static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern)
                              .Replace(@"\*", ".*")
                              .Replace(@"\?", ".")
                       + "$";
        }

        public override Task<IISBindingsOptions> Default()
        {
            var options = new IISBindingsOptions();
            var args = _arguments.GetArguments<IISBindingsArguments>();
            var filterSet = _helper.GetBindings();

            if (!string.IsNullOrEmpty(args.Simple))
            {
                var regEx = TryParseRegEx(WildcardToRegex(args.Simple));

                if (regEx != default)
                {
                    if (filterSet.Any(binding => regEx.IsMatch(binding.HostUnicode)))
                    {
                        options.Simple = args.Simple;
                    }
                    else
                    {
                        _log.Error("No matching host found with {search}", args.Simple);
                        return Task.FromResult(default(IISBindingsOptions));
                    }
                }
            }
            else if (!string.IsNullOrEmpty(args.RegEx))
            {
                var regEx = TryParseRegEx(args.RegEx);

                if (regEx != default)
                {
                    if (filterSet.Any(binding => regEx.IsMatch(binding.HostUnicode)))
                    {
                        options.RegEx = regEx;
                    }
                    else
                    {
                        _log.Error("No matching host found with {search}", args.RegEx);
                        return Task.FromResult(default(IISBindingsOptions));
                    }
                }
            }
            else
            {
                return Task.FromResult(default(IISBindingsOptions));
            }

            return Task.FromResult(options);
        }
    }
}