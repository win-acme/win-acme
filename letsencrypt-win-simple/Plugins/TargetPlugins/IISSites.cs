using LetsEncrypt.ACME.Simple.Services;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISSites : IISPlugin, ITargetPlugin
    {
        string ITargetPlugin.Name
        {
            get
            {
                return nameof(IISSites);
            }
        }

        Target ITargetPlugin.Default(Options options)
        {
            return null;
        }

        Target ITargetPlugin.Aquire(Options options)
        {
            var targets = GetSites().
                Select(x => new InputService.Choice<Target>(x) { description = x.Host }).
                ToList();
            return Program.Input.ChooseFromList("Choose sites", targets);
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