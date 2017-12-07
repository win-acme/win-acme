using Autofac;
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
        public override ITargetPluginFactory GetTargetPlugin(ILifetimeScope scope)
        {
            // List options for generating new certificates
            var ret = _input.ChooseFromList("Which kind of certificate would you like to create?",
                _plugins.TargetPluginFactories(scope),
                x => Choice.Create(x, description: x.Description),
                true);
            return ret ?? new NullTargetFactory();
        }

        /// <summary>
        /// Allow user to choose a ValidationPlugin
        /// </summary>
        /// <returns></returns>
        public override IValidationPluginFactory GetValidationPlugin(ILifetimeScope scope)
        {
            var ret = _input.ChooseFromList(
                "How would you like to validate this certificate?",
                _plugins.ValidationPluginFactories(scope).Where(x => x.CanValidate(_renewal.Binding)),
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
        public override List<IInstallationPluginFactory> GetInstallationPlugins(ILifetimeScope scope)
        {
            var ret = new List<IInstallationPluginFactory>();
            var ask = false;
            var filtered = _plugins.InstallationPluginFactories(scope).Where(x => x.CanInstall(_renewal));
            do
            {
                ask = false;
                var installer = _input.ChooseFromList(
                    "Which installer should run for the certificate?",
                    filtered,
                    x => Choice.Create(x, description: x.Description),
                    true);
                if (installer != null)
                {
                    ret.Add(installer);
                    if (!(installer is INull))
                    {
                        filtered = filtered.Where(x => !ret.Contains(x)).Where(x => !(x is INull));
                        if (filtered.Count() > 0)
                        {
                            ask = _input.PromptYesNo("Would you like to add another installer step?");
                        }
                    }
                }          
            } while (ask);
            return ret;
        }
    }
}
