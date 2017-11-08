using LetsEncrypt.ACME.Simple.Plugins.InstallationPlugins;
using LetsEncrypt.ACME.Simple.Plugins.TargetPlugins;
using LetsEncrypt.ACME.Simple.Plugins.ValidationPlugins;
using LetsEncrypt.ACME.Simple.Services;
using System.Collections.Generic;
using System.Linq;

namespace LetsEncrypt.ACME.Simple.Plugins
{
    public class InteractiveResolver : UnattendedResolver
    {
        private ScheduledRenewal _renewal;
        private PluginService _plugins;
        private ILogService _log;
        private IInputService _input;

        public InteractiveResolver(ScheduledRenewal renewal, ILogService log, IInputService inputService, PluginService pluginService) 
            : base(renewal, log, pluginService)
        {
            _renewal = renewal;
            _log = log;
            _input = inputService;
            _plugins = pluginService;
        }

        /// <summary>
        /// Allow user to choose a TargetPlugin
        /// </summary>
        /// <returns></returns>
        public override ITargetPluginFactory GetTargetPlugin()
        {
            // List options for generating new certificates
            var ret = _input.ChooseFromList("Which kind of certificate would you like to create?",
                _plugins.Target,
                x => Choice.Create(x, description: x.Description),
                true);
            return ret ?? new NullTargetFactory();
        }

        /// <summary>
        /// Allow user to choose a ValidationPlugin
        /// </summary>
        /// <returns></returns>
        public override IValidationPluginFactory GetValidationPlugin()
        {
            var ret = _input.ChooseFromList(
                "How would you like to validate this certificate?",
                _plugins.Validation.Where(x => x.CanValidate(_renewal.Binding)),
                x => Choice.Create(x, description: $"[{x.ChallengeType}] {x.Description}"),
                true);
            return ret ?? new NullValidationFactory();
        }

        /// <summary>
        /// Allow user to choose a InstallationPlugins
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<IInstallationPluginFactory> GetInstallationPlugins()
        {
            var ret = new List<IInstallationPluginFactory>();
            var ask = _input.PromptYesNo("Would you like to install this certificate to your server software?");
            var filtered = _plugins.Installation.Where(x => x.CanInstall(_renewal));
            do
            {
                ask = false;
                var installer = _input.ChooseFromList(
                    "Which plugin will run for the certificate?",
                    filtered,
                    x => Choice.Create(x, description: x.Description),
                    true);
                if (installer != null)
                {
                    ret.Add(installer);
                    filtered = filtered.Where(x => !ret.Contains(x));
                    if (filtered.Count() > 0)
                    {
                        ask = _input.PromptYesNo("Would you like to add another installation step?");
                    }
                }               
            } while (ask);
            return ret;
        }
    }
}
