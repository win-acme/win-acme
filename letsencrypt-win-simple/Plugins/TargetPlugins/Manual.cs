using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class ManualFactory : BaseTargetPluginFactory<Manual>
    {
        public ManualFactory() : base(nameof(Manual), "Manually input host names") { }
    }

    class Manual : ITargetPlugin
    {
        private ILogService _log;

        public Manual(ILogService logService)
        {
            _log = logService;
        }

        Target ITargetPlugin.Default(IOptionsService optionsService)
        {
            var host = optionsService.TryGetRequiredOption(nameof(optionsService.Options.ManualHost), optionsService.Options.ManualHost);
            return Create(ParseSanList(host));
        }

        Target ITargetPlugin.Aquire(IOptionsService optionsService, IInputService inputService)
        {
            List<string> sanList = ParseSanList(inputService.RequestString("Enter comma-separated list of host names, starting with the primary one"));
            if (sanList != null)
            {
                return Create(sanList);
            }
            return null;
        }

        Target Create(List<string> sanList)
        {
            return new Target()
            {
                Host = sanList.First(),
                HostIsDns = true,
                AlternativeNames = sanList
            };
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
                                Select(x => x.Trim().ToLower()).
                                Distinct());
            }
            if (ret.Count > SettingsService.maxNames)
            {
                _log.Error($"You entered too many hosts for a single certificate. Let's Encrypt currently has a maximum of {SettingsService.maxNames} alternative names per certificate.");
                return null;
            }
            if (ret.Count == 0)
            {
                _log.Error("No host names provided.");
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