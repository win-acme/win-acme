using LetsEncrypt.ACME.Simple.Clients;
using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class Manual : ScriptClient, ITargetPlugin
    {
        string IHasName.Name => nameof(Manual);
        string IHasName.Description => "Manually input host names";

        Target ITargetPlugin.Default(IOptionsService options)
        {
            var host = options.TryGetRequiredOption(nameof(options.Options.ManualHost), options.Options.ManualHost);
            return Create(options, ParseSanList(host));
        }

        Target ITargetPlugin.Aquire(IOptionsService options, IInputService input)
        {
            List<string> sanList = ParseSanList(input.RequestString("Enter comma-separated list of host names, starting with the primary one"));
            if (sanList != null)
            {
                return Create(options, sanList);
            }
            return null;
        }

        Target Create(IOptionsService options, List<string> sanList)
        {
            return new Target()
            {
                Host = sanList.First(),
                HostIsDns = true,
                AlternativeNames = sanList,
                PluginName = InstallationPlugins.RunScript.PluginName
            };
        }

        Target ITargetPlugin.Refresh(IOptionsService options, Target scheduled)
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