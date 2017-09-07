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

        string ITargetPlugin.Description
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
                var fqdns = ParseSanList(options.ManualHost);
                if (fqdns.Count > Settings.maxNames)
                {
                    Program.Log.Error("Too many hosts for a single certificate. Let's Encrypt has a maximum of {maxNames} per certificate.", Settings.maxNames);
                    return null;
                }
                else if (fqdns.Count == 0)
                {
                    Program.Log.Error("No hosts specified.");
                    return null;
                }
                return new Target()
                {
                    Host = fqdns.First(),
                    WebRootPath = options.WebRoot,
                    AlternativeNames = fqdns
                };           
            }
            return null;
        }

        Target ITargetPlugin.Aquire(Options options)
        {
            return InputTarget(nameof(Manual), new[] { "Enter a site path (the web root of the host for http authentication)" });
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            return scheduled;
        }
    }
}