using LetsEncrypt.ACME.Simple.Services;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSite : IISPlugin, ITargetPlugin
    {
        string IHasName.Name
        {
            get
            {
                return nameof(IISSite);
            }
        }

        string ITargetPlugin.Description
        {
            get
            {
                return "All bindings for a single IIS site";
            }
        }

        Target ITargetPlugin.Default(Options options)
        {
            return null;
        }

        Target ITargetPlugin.Aquire(Options options)
        {
            return Program.Input.ChooseFromList("Choose site",
                GetSites(),
                x => new InputService.Choice<Target>(x) { description = x.Host },
                true);
        }

        Target ITargetPlugin.Refresh(Options options, Target scheduled)
        {
            var match = GetSites().FirstOrDefault(binding => binding.Host == scheduled.Host);
            if (match != null)
            {
                UpdateWebRoot(scheduled, match);
                UpdateAlternativeNames(scheduled, match);
                return scheduled;
            }
            return null;
        }
    }
}