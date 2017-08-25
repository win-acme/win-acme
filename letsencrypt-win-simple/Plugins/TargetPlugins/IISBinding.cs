using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins.TargetPlugins
{
    class IISBinding : IISPlugin, ITargetPlugin
    {
        Target ITargetPlugin.Aquire
        {
            get
            {
                var targets = GetTargets().
                    Select(x => new Services.InputService.Choice<Target>(x) { description = x.Host }).
                    ToList();
                return Program.Input.ChooseFromList("Choose binding", targets);
            }
        }

        string ITargetPlugin.Name
        {
            get
            {
                return nameof(IISBinding);
            }
        }

        Target ITargetPlugin.Refresh(Target scheduled)
        {
            var match = GetTargets().FirstOrDefault(binding => binding.Host == scheduled.Host);
            if (match != null)
            {
                return UpdateWebRoot(scheduled, match);
            }
            return null;
        }
    }
}
