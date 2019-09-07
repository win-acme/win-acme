using Autofac;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PKISharp.WACS.Plugins.Resolvers
{
    public class UnattendedResolver : IResolver
    {
        private readonly PluginService _plugins;
        protected IArgumentsService _options;
        private readonly ILogService _log;

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
        public virtual IInstallationPluginOptionsFactory GetInstallationPlugin(ILifetimeScope scope, IEnumerable<Type> storeTypes, IEnumerable<IInstallationPluginOptionsFactory> chosen)
        {
            if (string.IsNullOrEmpty(_options.MainArguments.Installation))
            {
                return new NullInstallationOptionsFactory();
            }
            else
            {
                var parts = _options.MainArguments.Installation.ParseCsv();
                var index = chosen.Count();
                if (index == parts.Count)
                {
                    return new NullInstallationOptionsFactory();
                }

                var name = parts[index];
                var factory = _plugins.InstallationPluginFactory(scope, name);
                if (factory == null)
                {
                    _log.Error("Unable to find installation plugin {PluginName}", name);
                    return null;
                }
                if (!factory.CanInstall(storeTypes))
                {
                    _log.Error("Installation plugin {PluginName} cannot install from selected store(s)", name);
                    return null;
                }
                return factory;
            }
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public virtual IStorePluginOptionsFactory GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen)
        {
            if (string.IsNullOrEmpty(_options.MainArguments.Store))
            {
                if (chosen.Count() == 0)
                {
                    return _plugins.StorePluginFactory(scope, CertificateStoreOptions.PluginName);
                }
                else
                {
                    return new NullStoreOptionsFactory();
                }
            }
            else
            {
                var parts = _options.MainArguments.Store.ParseCsv();
                var index = chosen.Count();
                if (index == parts.Count)
                {
                    return new NullStoreOptionsFactory();
                }

                var name = parts[index];
                var factory = _plugins.StorePluginFactory(scope, name);
                if (factory == null)
                {
                    _log.Error("Unable to find store plugin {PluginName}", name);
                    return null;
                }
                return factory;
            }
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
