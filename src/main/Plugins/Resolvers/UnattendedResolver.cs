using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System.Collections.Generic;

namespace PKISharp.WACS.Plugins.Resolvers
{
    public class UnattendedResolver : IResolver
    {
        private PluginService _plugins;
        protected IArgumentsService _options;
        private ILogService _log;

        public UnattendedResolver(ILogService log, IArgumentsService options, PluginService pluginService)
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
            var targetPluginFactory = _plugins.TargetPluginFactory(scope, _options.MainArguments.Target);
            if (targetPluginFactory == null)
            {
                _log.Error("Unable to find target plugin {PluginName}", _options.MainArguments.Target);
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
            if (string.IsNullOrEmpty(_options.MainArguments.Validation))
            {
                validationPluginFactory = scope.Resolve<SelfHostingOptionsFactory>();
            }
            else
            {
                validationPluginFactory = _plugins.ValidationPluginFactory(scope, _options.MainArguments.ValidationMode, _options.MainArguments.Validation);
            }
            if (validationPluginFactory == null)
            {
                _log.Error("Unable to find validation plugin {PluginName}", _options.MainArguments.Validation);
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
        public virtual List<IInstallationPluginOptionsFactory> GetInstallationPlugins(ILifetimeScope scope, string storeType)
        {
            var ret = new List<IInstallationPluginOptionsFactory>();
            if (string.IsNullOrEmpty(_options.MainArguments.Installation))
            {
                ret.Add(_plugins.InstallationPluginFactory(scope, "None"));
            }
            else
            {
                foreach (var name in _options.MainArguments.Installation.ParseCsv())
                {
                    var installationPluginFactory = _plugins.InstallationPluginFactory(scope, name);
                    if (installationPluginFactory == null)
                    {
                        _log.Error("Unable to find installation plugin {PluginName}", name); 
                        // Make sure that no partial results are returned
                        return new List<IInstallationPluginOptionsFactory>();
                    }
                    else if (!installationPluginFactory.CanInstall(storeType))
                    {
                        _log.Error("Installation plugin {PluginName} cannot install from selected store", name);
                        // Make sure that no partial results are returned
                        return new List<IInstallationPluginOptionsFactory>();
                    }
                    else
                    {
                        ret.Add(installationPluginFactory);
                    }
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
            var pluginName = _options.MainArguments.Store;
            if (string.IsNullOrEmpty(pluginName))
            {
                pluginName = CertificateStoreOptions.PluginName;
            }
            var ret = _plugins.StorePluginFactory(scope, pluginName);
            if (ret == null)
            {
                _log.Error("Unable to find store plugin {PluginName}", pluginName);
                return new NullStoreFactory();
            }
            return ret;
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual ICsrPluginOptionsFactory GetCsrPlugin(ILifetimeScope scope)
        {
            var pluginName = _options.MainArguments.Csr;
            if (string.IsNullOrEmpty(pluginName))
            {
                return scope.Resolve<RsaOptionsFactory>();
            }
            var ret = _plugins.CsrPluginFactory(scope, pluginName);
            if (ret == null)
            {
                _log.Error("Unable to find csr plugin {PluginName}", pluginName);
                return new NullCsrFactory();
            }
            return ret;
        }
    }
}
