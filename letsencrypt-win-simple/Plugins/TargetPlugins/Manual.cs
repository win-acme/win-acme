using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class Manual : ManualPlugin, ITargetPlugin
    {
        string ITargetPlugin.Name
        {
            get
            {
                return nameof(Manual);
            }
        }

        Target ITargetPlugin.Default(Options options)
        {
            if (!string.IsNullOrEmpty(options.ManualHost))
            {
                var fqdns = new List<string>();
                fqdns = options.ManualHost.Split(',').ToList();
                fqdns = fqdns.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList();
                if (fqdns.Count > 0)
                {
                    return new Target()
                    {
                        Host = fqdns.First(),
                        WebRootPath = options.WebRoot,
                        AlternativeNames = fqdns
                    };
                }             
            }
            return null;
        }

        Target ITargetPlugin.Aquire(Options options)
        {
            var targets = GetSites().
                Select(x => new InputService.Choice<Target>(x) { description = x.Host }).
                ToList();
            return Program.Input.ChooseFromList("Choose site", targets);
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            return scheduled;
        }
    }
}