using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;
using PKISharp.WACS.Extensions;

namespace PKISharp.WACS.Plugins.TargetPlugins
{
    internal class ManualFactory : BaseTargetPluginFactory<Manual>
    {
        public ManualFactory(ILogService log) : base(log, nameof(Manual), "Manually input host names") { }
    }

    internal class Manual : ITargetPlugin
    {
        private ILogService _log;

        public Manual(ILogService logService)
        {
            _log = logService;
        }

        Target ITargetPlugin.Default(IOptionsService optionsService)
        {
            var input = optionsService.TryGetRequiredOption(nameof(optionsService.Options.ManualHost), optionsService.Options.ManualHost);
            var target = Create(input);
            target.CommonName = optionsService.Options.CommonName;
            if (!target.IsCommonNameValid(_log)) return null;
            return target;
        }

        Target ITargetPlugin.Aquire(IOptionsService optionsService, IInputService inputService, RunLevel runLevel)
        {
            var input = inputService.RequestString("Enter comma-separated list of host names, starting with the primary one");
            var target = Create(input);
            if (runLevel >= RunLevel.Advanced) target.AskForCommonNameChoice(inputService);
            return target;
        }

        private Target Create(string input)
        {
            var sanList = ParseSanList(input);
            if (sanList != null)
            {
                return new Target()
                {
                    Host = sanList.First(),
                    HostIsDns = true,
                    AlternativeNames = sanList
                };
            }
            else
            {
                return null;
            }
        }

        Target ITargetPlugin.Refresh(Target scheduled)
        {
            return scheduled;
        }

        private List<string> ParseSanList(string input)
        {
            var ret = new List<string>();
            if (!string.IsNullOrEmpty(input))
            {
                ret.AddRange(input.
                                ToLower().
                                Split(',').
                                Where(x => !string.IsNullOrWhiteSpace(x)).
                                Where(x => !x.StartsWith("*")).
                                Select(x => x.Trim().ToLower()).
                                Distinct());
            }
            if (ret.Count > Constants.maxNames)
            {
                _log.Error($"You entered too many hosts for a single certificate. ACME currently has a maximum of {Constants.maxNames} alternative names per certificate.");
                return null;
            }
            if (ret.Count == 0)
            {
                _log.Error("No (valid) host names provided.");
                return null;
            }
            return ret;
        }

        public IEnumerable<Target> Split(Target scheduled)
        {
            return new List<Target> { scheduled };
        }
    }
}