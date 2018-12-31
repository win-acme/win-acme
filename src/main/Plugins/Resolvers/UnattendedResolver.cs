using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.InstallationPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Resolvers
{
    public class UnattendedResolver : IResolver
    {
        private PluginService _plugins;
        private IOptionsService _options;
        private ILogService _log;

        public UnattendedResolver(ILogService log, IOptionsService options, PluginService pluginService)
        {
            _log = log;
            _plugins = pluginService;
            _options = options;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual ITargetPluginOptionsFactory GetTargetPlugin(ILifetimeScope scope)
        {
            // Get plugin factory
            var targetPluginFactory = _plugins.TargetPluginFactory(scope, _options.Options.Target);
            if (targetPluginFactory == null)
            {
                _log.Error("Unable to find target plugin {PluginName}", _options.Options.Target);
                return new NullTargetFactory(); 
            }
            return targetPluginFactory;
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual IValidationPluginOptionsFactory GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            // Get plugin factory
            IValidationPluginOptionsFactory validationPluginFactory;
            if (string.IsNullOrEmpty(_options.Options.Validation))
            {
                validationPluginFactory = scope.Resolve<SelfHostingOptionsFactory>();
            }
            else
            {
                validationPluginFactory = _plugins.ValidationPluginFactory(scope, _options.Options.ValidationMode, _options.Options.Validation);
            }
            if (validationPluginFactory == null)
            {
                _log.Error("Unable to find validation plugin {PluginName}", _options.Options.Validation);
                return new NullValidationFactory();
            }
            if (!validationPluginFactory.CanValidate(target))
            {
                _log.Error("Validation plugin {PluginName} cannot validate this target", validationPluginFactory.Name);
                return new NullValidationFactory();
            }
            return validationPluginFactory;
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual List<IInstallationPluginOptionsFactory> GetInstallationPlugins(ILifetimeScope scope)
        {
            var ret = new List<IInstallationPluginOptionsFactory>();
            foreach (var name in _options.Options.Installation)
            {
                var installationPluginFactory = _plugins.InstallationPluginFactory(scope, name);
                if (installationPluginFactory == null)
                {
                    _log.Error("Unable to find installation plugin {PluginName}", name);
                }
                else
                {
                    ret.Add(installationPluginFactory);
                }
            }
            return ret;
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public virtual IStorePluginOptionsFactory GetStorePlugin(ILifetimeScope scope)
        {
            var pluginName = _options.Options.Store;
            var ret = _plugins.StorePluginFactory(scope, pluginName);
            if (ret == null)
            {
                _log.Error("Unable to find store plugin {PluginName}", pluginName);
                return new NullStoreFactory();
            }
            return ret;
        }
    }
}
