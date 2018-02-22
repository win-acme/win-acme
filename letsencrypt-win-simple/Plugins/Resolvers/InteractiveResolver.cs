using Autofac;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.Resolvers
{
    public class InteractiveResolver : UnattendedResolver
    {
        private ScheduledRenewal _renewal;
        private PluginService _plugins;
        private ILogService _log;
        private IInputService _input;
        private RunLevel _runLevel;

        public InteractiveResolver(ScheduledRenewal renewal,
            ILogService log,
            IInputService inputService,
            PluginService pluginService,
            RunLevel runLevel)
            : base(renewal, log, pluginService)
        {
            _renewal = renewal;
            _log = log;
            _input = inputService;
            _plugins = pluginService;
            _runLevel = runLevel;
        }

        /// <summary>
        /// Allow user to choose a TargetPlugin
        /// </summary>
        /// <returns></returns>
        public override ITargetPluginFactory GetTargetPlugin(ILifetimeScope scope)
        {
            // List options for generating new certificates
            var options = _plugins.TargetPluginFactories(scope).Where(x => !x.Hidden);
            var ret = _input.ChooseFromList("Which kind of certificate would you like to create?",
                _plugins.TargetPluginFactories(scope).Where(x => !x.Hidden),
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
            if (_runLevel == RunLevel.Advanced)
            {
                var ret = _input.ChooseFromList(
                    "How would you like to validate this certificate?",
                    _plugins.ValidationPluginFactories(scope).Where(x => !x.Hidden && x.CanValidate(_renewal.Binding)),
                    x => Choice.Create(x, description: $"[{x.ChallengeType}] {x.Description}"),
                    true);
                return ret ?? new NullValidationFactory();
            }
            else
            {
                return scope.Resolve<SelfHostingFactory>();
            }
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
            if (_runLevel == RunLevel.Advanced)
            {
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
               
            }
            else
            {
                ret.Add(scope.Resolve<IISInstallerFactory>());
            }
            return ret;

        }
    }
}
