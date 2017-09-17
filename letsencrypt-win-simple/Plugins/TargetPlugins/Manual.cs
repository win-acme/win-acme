using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class Manual : ManualPlugin, ITargetPlugin
    {
        string IHasName.Name
        {
            get
            {
                return nameof(Manual);
            }
        }

        string IHasName.Description
        {
            get
            {
                return "Manually input host names";
            }
        }

        Target ITargetPlugin.Default(Options options)
        {
            if (!string.IsNullOrEmpty(options.ManualHost))
            {
                return Create(options, ParseSanList(options.ManualHost));
            }
            return null;
        }

        Target ITargetPlugin.Aquire(Options options, InputService input)
        {
            List<string> sanList = ParseSanList(input.RequestString("Enter comma-separated list of host names, starting with the primary one"));
            if (sanList != null)
            {
                return Create(options, sanList);
            }
            return null;
        }

        Target Create(Options options, List<string> sanList)
        {
            return new Target()
            {
                Host = sanList.First(),
                HostIsDns = true,
                IIS = options.ManualTargetIsIIS,
                AlternativeNames = sanList,
                PluginName = PluginName
            };
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
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
                                Select(x => x.Trim()).
                                Distinct());
            }
            if (ret.Count > Settings.maxNames)
            {
                Program.Log.Error($"You entered too many hosts for a single certificate. Let's Encrypt currently has a maximum of {Settings.maxNames} alternative names per certificate.");
                return null;
            }
            if (ret.Count == 0)
            {
                Program.Log.Error("No host names provided.");
                return null;
            }
            return ret;
        }
    }
}