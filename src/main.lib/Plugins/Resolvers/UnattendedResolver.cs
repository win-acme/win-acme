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
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Resolvers
{
    public class UnattendedResolver : IResolver
    {
        private readonly IPluginService _plugins;
        protected IArgumentsService _options;
        private readonly ILogService _log;

        public UnattendedResolver(ILogService log, IArgumentsService options, IPluginService pluginService)
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
        public virtual async Task<ITargetPluginOptionsFactory> GetTargetPlugin(ILifetimeScope scope)
        {
            // Get plugin factory
            if (string.IsNullOrEmpty(_options.MainArguments.Target))
            {
                return new NullTargetFactory();
            }
            var targetPluginFactory = _plugins.TargetPluginFactory(scope, _options.MainArguments.Target);
            if (targetPluginFactory == null)
            {
                _log.Error("Unable to find target plugin {PluginName}", _options.MainArguments.Target);
                return new NullTargetFactory();
            }
            var (disabled, disabledReason) = targetPluginFactory.Disabled;
            if (disabled)
            {
                _log.Error($"Target plugin {{PluginName}} is not available. {disabledReason}", _options.MainArguments.Target);
                return new NullTargetFactory();
            }
            return targetPluginFactory;
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IValidationPluginOptionsFactory> GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            // Get plugin factory
            var validationPluginFactory = string.IsNullOrEmpty(_options.MainArguments.Validation)
                ? scope.Resolve<SelfHostingOptionsFactory>()
                : _plugins.ValidationPluginFactory(scope,
                    _options.MainArguments.ValidationMode ?? Constants.Http01ChallengeType, 
                    _options.MainArguments.Validation);

            if (validationPluginFactory == null)
            {
                _log.Error("Unable to find validation plugin {PluginName}", _options.MainArguments.Validation);
                return new NullValidationFactory();
            }
            var (disabled, disabledReason) = validationPluginFactory.Disabled;
            if (disabled)
            {
                _log.Error($"Validation plugin {{PluginName}} is not available. {disabledReason}", validationPluginFactory.Name);
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
        public virtual async Task<IInstallationPluginOptionsFactory?> GetInstallationPlugin(ILifetimeScope scope, IEnumerable<Type> storeTypes, IEnumerable<IInstallationPluginOptionsFactory> chosen)
        {
            if (string.IsNullOrEmpty(_options.MainArguments.Installation))
            {
                return new NullInstallationOptionsFactory();
            }
            else
            {
                var parts = _options.MainArguments.Installation.ParseCsv();
                var index = chosen.Count();
                if (parts == null || index == parts.Count)
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
                var (disabled, disabledReason) = factory.Disabled;
                if (disabled)
                {
                    _log.Error($"Installation plugin {{PluginName}} is not available. {disabledReason}", name);
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
        public virtual async Task<IStorePluginOptionsFactory?> GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen)
        {
            var args = _options.MainArguments.Store;
            if (string.IsNullOrEmpty(args))
            {
                if (chosen.Count() == 0)
                {
                    args = CertificateStoreOptions.PluginName;
                }
                else
                {
                    return new NullStoreOptionsFactory();
                }
            }

            var parts = args.ParseCsv();
            if (parts == null)
            {
                return null;
            }

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
            var (disabled, disabledReason) = factory.Disabled;
            if (disabled)
            {
                _log.Error($"Store plugin {{PluginName}} is not available. {disabledReason}", name);
                return null;
            }
            return factory;
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ICsrPluginOptionsFactory> GetCsrPlugin(ILifetimeScope scope)
        {
            var pluginName = _options.MainArguments.Csr;
            if (string.IsNullOrEmpty(pluginName))
            {
                return scope.Resolve<RsaOptionsFactory>();
            }
            var factory = _plugins.CsrPluginFactory(scope, pluginName);
            if (factory == null)
            {
                _log.Error("Unable to find csr plugin {PluginName}", pluginName);
                return new NullCsrFactory();
            }
            var (disabled, disabledReason) = factory.Disabled;
            if (disabled)
            {
                _log.Error($"CSR plugin {{PluginName}} is not available. {disabledReason}", pluginName);
                return new NullCsrFactory();
            }
            return factory;
        }
    }
}
