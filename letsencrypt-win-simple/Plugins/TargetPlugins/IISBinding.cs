using LetsEncrypt.ACME.Simple.Services;
using System.Linq;
using System;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISBinding : IISPlugin, ITargetPlugin
    {
        string IHasName.Name
        {
            get
            {
                return nameof(IISBinding);
            }
        }

        string ITargetPlugin.Description
        {
            get
            {
                return "Single binding of an IIS site";
            }
        }

        Target ITargetPlugin.Default(Options options)
        {
            return null;
        }

        Target ITargetPlugin.Aquire(Options options)
        {
            return Program.Input.ChooseFromList("Choose site",
                GetBindings(),
                x => InputService.Choice.Create(x, description: $"{x.Host} (SiteId {x.SiteId}) [@{x.WebRootPath}]"),
                true);
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            var match = GetBindings().FirstOrDefault(binding => binding.Host == scheduled.Host);
            if (match != null)
            {
                UpdateWebRoot(scheduled, match);
                return scheduled;
            }
            return null;
        }
    }
}
