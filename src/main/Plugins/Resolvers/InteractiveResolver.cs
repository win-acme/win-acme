using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories.Null;
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
        private PluginService _plugins;
        private ILogService _log;
        private IInputService _input;
        private RunLevel _runLevel;

        public InteractiveResolver(
            ILogService log,
            IInputService inputService,
            IOptionsService optionsService,
            PluginService pluginService,
            RunLevel runLevel)
            : base(log, optionsService, pluginService)
        {
            _log = log;
            _input = inputService;
            _plugins = pluginService;
            _runLevel = runLevel;
        }

        /// <summary>
        /// Allow user to choose a TargetPlugin
        /// </summary>
        /// <returns></returns>
        public override ITargetPluginOptionsFactory GetTargetPlugin(ILifetimeScope scope)
        {
            // List options for generating new certificates
            var options = _plugins.TargetPluginFactories(scope).Where(x => !x.Hidden).OrderBy(x => x.Description);
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
        public override IValidationPluginOptionsFactory GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            if (_runLevel.HasFlag(RunLevel.Advanced))
            {
                var ret = _input.ChooseFromList(
                    "How would you like to validate this certificate?",
                    _plugins.ValidationPluginFactories(scope).
                        Where(x => !(x is INull)).
                        Where(x => x.CanValidate(target)).
                        OrderBy(x => x.ChallengeType + x.Description),
                    x => Choice.Create(x, description: $"[{x.ChallengeType}] {x.Description}"),
                    true);
                return ret ?? new NullValidationFactory();
            }
            else
            {
                var ret = scope.Resolve<SelfHostingOptionsFactory>();
                if (ret.CanValidate(target))
                {
                    return ret;
                }
                else
                {
                    _log.Error("The default validation plugin cannot be " +
                        "used for this target. Most likely this is because " +
                        "you have included a wildcard identifier (*.example.com), " +
                        "which requires DNS validation. Choose another plugin " +
                        "from the advanced menu ('M').");
                    return new NullValidationFactory();
                }
            }
        }

        /// <summary>
        /// Allow user to choose a InstallationPlugins
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// </summary>
        /// <returns></returns>
        public override List<IInstallationPluginOptionsFactory> GetInstallationPlugins(ILifetimeScope scope)
        {
            var ret = new List<IInstallationPluginOptionsFactory>();
            if (_runLevel.HasFlag(RunLevel.Advanced))
            {
                var ask = false;
                var filtered = _plugins.InstallationPluginFactories(scope).Where(x => x.CanInstall()).OrderBy(x => x.Description);
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
                            filtered = filtered.Where(x => !ret.Contains(x)).Where(x => !(x is INull)).OrderBy(x => x.Description);
                            if (filtered.Any())
                            {
                                ask = _input.PromptYesNo("Would you like to add another installer step?");
                            }
                        }
                    }
                } while (ask);
               
            }
            else
            {
                ret.Add(scope.Resolve<IISWebOptionsFactory>());
            }
            return ret;

        }
    }
}
