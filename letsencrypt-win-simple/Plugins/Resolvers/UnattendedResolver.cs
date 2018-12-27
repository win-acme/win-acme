using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Plugins.Base.Factories;
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
        private ScheduledRenewal _renewal;
        private PluginService _plugins;
        private IOptionsService _options;
        private ILogService _log;

        public UnattendedResolver(ScheduledRenewal renewal, ILogService log, IOptionsService options, PluginService pluginService)
        {
            _renewal = renewal;
            _log = log;
            _plugins = pluginService;
            _options = options;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual ITargetPluginFactory GetTargetPlugin(ILifetimeScope scope)
        {
            // Get plugin factory
            var targetPluginFactory = _plugins.TargetPluginFactory(scope, _renewal.Target.TargetPluginName);
            if (targetPluginFactory == null)
            {
                _log.Error("Unable to find target plugin {PluginName}", _renewal.Target.TargetPluginName);
                return new NullTargetFactory(); 
            }
            return targetPluginFactory;
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual IValidationPluginFactory GetValidationPlugin(ILifetimeScope scope)
        {
            // Get plugin factory
            if (string.IsNullOrEmpty(_options.Options.Validation))
            {           
                return scope.Resolve<SelfHostingFactory>();
            }
            var validationPluginFactory = _plugins.ValidationPluginFactory(scope, _options.Options.ValidationMode, _options.Options.Validation);
            if (validationPluginFactory == null)
            {
                _log.Error("Unable to find validation plugin {PluginName}", _options.Options.Validation);
                return new NullValidationFactory();
            }
            return validationPluginFactory;
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual List<IInstallationPluginFactory> GetInstallationPlugins(ILifetimeScope scope)
        {
            if (_renewal.InstallationPluginNames == null)
            {
                _renewal.InstallationPluginNames = new List<string>();
                // Based on chosen target
                if (_renewal.Target.TargetPluginName == nameof(IISSite) ||
                    _renewal.Target.TargetPluginName == nameof(IISSites) ||
                    _renewal.Target.TargetPluginName == nameof(IISBinding))
                {
                    _renewal.InstallationPluginNames.Add(IISWebInstallerFactory.PluginName);
                }
                
                // Based on command line
                if (!string.IsNullOrEmpty(_renewal.Script) || !string.IsNullOrEmpty(_renewal.ScriptParameters))
                {
                    _renewal.InstallationPluginNames.Add(ScriptInstallerFactory.PluginName);
                }

                // Cannot find anything, then it's no installation steps
                if (_renewal.InstallationPluginNames.Count == 0)
                {
                    _renewal.InstallationPluginNames.Add(NullInstallationFactory.PluginName);
                }
            }

            // Get plugin factory
            var ret = new List<IInstallationPluginFactory>();
            foreach (var name in _renewal.InstallationPluginNames)
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
        public virtual IStorePluginFactory GetStorePlugin(ILifetimeScope scope)
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
