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
        public virtual Task<ITargetPluginOptionsFactory> GetTargetPlugin(ILifetimeScope scope)
        {
            // Get plugin factory
            var nullResult = Task.FromResult<ITargetPluginOptionsFactory>(new NullTargetFactory());
            var targetPluginFactory = _plugins.TargetPluginFactory(scope, _options.MainArguments.Target);
            if (targetPluginFactory == null)
            {
                _log.Error("Unable to find target plugin {PluginName}", _options.MainArguments.Target);
                return nullResult;
            }
            if (targetPluginFactory.Disabled)
            {
                _log.Error("Target plugin {PluginName} is not available to the current user, try running as administrator", _options.MainArguments.Target);
                return nullResult;
            }
            return Task.FromResult(targetPluginFactory);
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual Task<IValidationPluginOptionsFactory> GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            // Get plugin factory
            var validationPluginFactory = string.IsNullOrEmpty(_options.MainArguments.Validation)
                ? scope.Resolve<SelfHostingOptionsFactory>()
                : _plugins.ValidationPluginFactory(scope, _options.MainArguments.ValidationMode, _options.MainArguments.Validation);

            var nullResult = Task.FromResult<IValidationPluginOptionsFactory>(new NullValidationFactory());

            if (validationPluginFactory == null)
            {
                _log.Error("Unable to find validation plugin {PluginName}", _options.MainArguments.Validation);
                return nullResult;
            }
            if (validationPluginFactory.Disabled)
            {
                _log.Error("Validation plugin {PluginName} is not available to the current user, try running as administrator", _options.MainArguments.Validation);
                return nullResult;
            }
            if (!validationPluginFactory.CanValidate(target))
            {
                _log.Error("Validation plugin {PluginName} cannot validate this target", validationPluginFactory.Name);
                return nullResult;
            }
            return Task.FromResult(validationPluginFactory);
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual Task<IInstallationPluginOptionsFactory> GetInstallationPlugin(ILifetimeScope scope, IEnumerable<Type> storeTypes, IEnumerable<IInstallationPluginOptionsFactory> chosen)
        {
            var nullResult = Task.FromResult<IInstallationPluginOptionsFactory>(new NullInstallationOptionsFactory());
            var nothingResult = Task.FromResult<IInstallationPluginOptionsFactory>(null);

            if (string.IsNullOrEmpty(_options.MainArguments.Installation))
            {
                return nullResult;
            }
            else
            {
                var parts = _options.MainArguments.Installation.ParseCsv();
                var index = chosen.Count();
                if (index == parts.Count)
                {
                    return nullResult;
                }

                var name = parts[index];
                var factory = _plugins.InstallationPluginFactory(scope, name);
                if (factory == null)
                {
                    _log.Error("Unable to find installation plugin {PluginName}", name);
                    return nothingResult;
                }
                if (factory.Disabled)
                {
                    _log.Error("Installation plugin {PluginName} is not available to the current user, try running as administrator", name);
                    return nothingResult;
                }
                if (!factory.CanInstall(storeTypes))
                {
                    _log.Error("Installation plugin {PluginName} cannot install from selected store(s)", name);
                    return nothingResult;
                }
                return Task.FromResult(factory);
            }
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public virtual Task<IStorePluginOptionsFactory> GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen)
        {
            var nullResult = Task.FromResult<IStorePluginOptionsFactory>(new NullStoreOptionsFactory());
            var nothingResult = Task.FromResult<IStorePluginOptionsFactory>(null);
            var args = _options.MainArguments.Store;
            if (string.IsNullOrEmpty(args))
            {
                if (chosen.Count() == 0)
                {
                    args = CertificateStoreOptions.PluginName;
                }
                else
                {
                    return nullResult;
                }
            }

            var parts = args.ParseCsv();
            var index = chosen.Count();
            if (index == parts.Count)
            {
                return nullResult;
            }

            var name = parts[index];
            var factory = _plugins.StorePluginFactory(scope, name);
            if (factory == null)
            {
                _log.Error("Unable to find store plugin {PluginName}", name);
                return nothingResult;
            }
            if (factory.Disabled)
            {
                _log.Error("Store plugin {PluginName} is not available to the current user, try running as administrator", name);
                return nothingResult;
            }
            return Task.FromResult(factory);
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual Task<ICsrPluginOptionsFactory> GetCsrPlugin(ILifetimeScope scope)
        {
            var pluginName = _options.MainArguments.Csr;
            var nothingResult = Task.FromResult<ICsrPluginOptionsFactory>(new NullCsrFactory());
            if (string.IsNullOrEmpty(pluginName))
            {
                return Task.FromResult<ICsrPluginOptionsFactory>(scope.Resolve<RsaOptionsFactory>());
            }
            var ret = _plugins.CsrPluginFactory(scope, pluginName);
            if (ret == null)
            {
                _log.Error("Unable to find csr plugin {PluginName}", pluginName);
                return nothingResult;
            }
            if (ret.Disabled)
            {
                _log.Error("CSR plugin {PluginName} is not available to the current user, try running as administrator", pluginName);
                return nothingResult;
            }
            return Task.FromResult(ret);
        }
    }
}
