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
                if (fqdns != null)
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
            return InputTarget(nameof(Manual), new[] { "Enter a site path (the web root of the host for http authentication)" });
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            return scheduled;
        }
    }
}