using LetsEncrypt.ACME.Simple.Services;
using System.Linq;
using System;
using LetsEncrypt.ACME.Simple.Clients;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISBinding : IISClient, ITargetPlugin
    {
        string IHasName.Name => nameof(IISBinding);
        string IHasName.Description => "Single binding of an IIS site";
        Target ITargetPlugin.Default(Options options) => null;

        Target ITargetPlugin.Aquire(Options options, InputService input)
        {
            return input.ChooseFromList("Choose site",
                GetBindings(options, true),
                x => InputService.Choice.Create(x, description: $"{x.Host} (SiteId {x.SiteId}) [@{x.WebRootPath}]"),
                true);
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            var match = GetBindings(options, false).FirstOrDefault(binding => binding.Host == scheduled.Host);
            if (match != null)
            {
                UpdateWebRoot(scheduled, match);
                return scheduled;
            }
            return null;
        }
    }
}
