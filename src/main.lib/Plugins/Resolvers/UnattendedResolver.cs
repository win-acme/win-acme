using Autofac;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.OrderPlugins;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.TargetPlugins;
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
        protected readonly MainArguments _arguments;
        protected readonly ISettingsService _settings;
        private readonly ILogService _log;

        public UnattendedResolver(ILogService log, ISettingsService settings, MainArguments arguments, IPluginService pluginService)
        {
            _log = log;
            _plugins = pluginService;
            _arguments = arguments;
            _settings = settings;
        }

        private async Task<T> GetPlugin<T>(
            ILifetimeScope scope,
            Type defaultType,
            T nullResult,
            string className,
            string? defaultParam1 = null,
            string? defaultParam2 = null,
            Func<IEnumerable<T>, IEnumerable<T>>? filter = null,
            Func<T, (bool, string?)>? unusable = null) where T: IPluginOptionsFactory
        {
            // Helper method to determine final usability state
            // combination of plugin being enabled (e.g. due to missing
            // administrator rights) and being a right fit for the current
            // renewal (e.g. cannot validate wildcards using http-01)
            (bool, string?) disabledOrUnusable(T plugin)
            {
                var disabled = plugin.Disabled;
                if (disabled.Item1)
                {
                    return disabled;
                }
                else if (unusable != null)
                {
                    return unusable(plugin);
                }
                return (false, null);
            };

            // Apply default sorting when no sorting has been provided yet
            var options = _plugins.GetFactories<T>(scope);
            options = filter != null ? filter(options) : options.Where(x => !(x is INull));
            var localOptions = options.Select(x => new {
                plugin = x,
                type = x.GetType(),
                disabled = disabledOrUnusable(x)
            });

            // Default out when there are no reasonable options to pick
            if (!localOptions.Any() ||
                localOptions.All(x => x.disabled.Item1) ||
                localOptions.All(x => x.plugin is INull))
            {
                return nullResult;
            }

            var changeInstructions = $"Choose another plugin using the --{className} switch or change the default in settings.json";
            if (!string.IsNullOrEmpty(defaultParam1))
            {
                var defaultPlugin = _plugins.GetFactory<T>(scope, defaultParam1, defaultParam2);
                if (defaultPlugin == null)
                {
                    _log.Error("Unable to find {n} plugin {p}. " + changeInstructions, className, defaultParam1);
                    return nullResult;
                }
                else
                {
                    defaultType = defaultPlugin.GetType();
                }
            }

            var defaultOption = localOptions.First(x => x.type == defaultType);
            var defaultTypeDisabled = defaultOption.disabled;
            if (defaultTypeDisabled.Item1)
            {
                _log.Error("{n} plugin {x} not available: {m}. " + changeInstructions, 
                    char.ToUpper(className[0]) + className.Substring(1), 
                    defaultOption.plugin?.Name ?? "Unknown",
                    defaultTypeDisabled.Item2?.TrimEnd('.'));
                return nullResult;
            }

            return (T)scope.Resolve(defaultType);
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// Renewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ITargetPluginOptionsFactory> GetTargetPlugin(ILifetimeScope scope)
        {
            // NOTE: checking the default option here doesn't make 
            // sense because MainArguments.Source is what triggers
            // unattended mode in the first place. We woudn't even 
            // get into this code unless it was specified.
            return await GetPlugin<ITargetPluginOptionsFactory>(
                scope,
                defaultParam1: string.IsNullOrWhiteSpace(_arguments.Source) ? _arguments.Target : _arguments.Source,
                defaultType: typeof(ManualOptionsFactory),
                nullResult: new NullTargetFactory(),
                className: "target");
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this Renewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IValidationPluginOptionsFactory> GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            return await GetPlugin<IValidationPluginOptionsFactory>(
                scope,
                defaultParam1: _arguments.Validation ?? 
                    _settings.Validation.DefaultValidation,
                defaultParam2: _arguments.ValidationMode ?? 
                    _settings.Validation.DefaultValidationMode ?? 
                    Constants.Http01ChallengeType,
                defaultType: typeof(SelfHostingOptionsFactory),
                nullResult: new NullValidationFactory(),
                unusable: (c) => (!c.CanValidate(target), "Unsuppored target. Most likely this is because you have included a wildcard identifier (*.example.com), which requires DNS validation."),
                className: "validation");
        }

        /// <summary>
        /// Get the OrderPlugin which is used to convert the target into orders 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IOrderPluginOptionsFactory> GetOrderPlugin(ILifetimeScope scope, Target target)
        {
            return await GetPlugin<IOrderPluginOptionsFactory>(
                scope,
                defaultParam1: _arguments.Order,
                defaultType: typeof(SingleOptionsFactory),
                nullResult: new NullOrderOptionsFactory(),
                className: "order");
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<ICsrPluginOptionsFactory> GetCsrPlugin(ILifetimeScope scope)
        {
            return await GetPlugin<ICsrPluginOptionsFactory>(
                scope,
                defaultParam1: _arguments.Csr,
                defaultType: typeof(RsaOptionsFactory),
                nullResult: new NullCsrFactory(),
                className: "csr");
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IStorePluginOptionsFactory?> GetStorePlugin(ILifetimeScope scope, IEnumerable<IStorePluginOptionsFactory> chosen)
        {
            var cmd = _arguments.Store ?? _settings.Store.DefaultStore;
            if (string.IsNullOrEmpty(cmd))
            {
                cmd = CertificateStoreOptions.PluginName;
            }
            var parts = cmd.ParseCsv();
            if (parts == null)
            {
                return null;
            }
            var index = chosen.Count();
            if (index == parts.Count)
            {
                return new NullStoreOptionsFactory();
            }
            return await GetPlugin<IStorePluginOptionsFactory>(
                scope,
                filter: x => x,
                defaultParam1: parts[index],
                defaultType: typeof(NullStoreOptionsFactory),
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                nullResult: default,
#pragma warning restore CS8625
                className: "store");
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<IInstallationPluginOptionsFactory?> GetInstallationPlugin(ILifetimeScope scope, IEnumerable<Type> storeTypes, IEnumerable<IInstallationPluginOptionsFactory> chosen)
        {
            var cmd = _arguments.Installation ?? _settings.Installation.DefaultInstallation;
            var parts = cmd.ParseCsv();
            if (parts == null)
            {
                return new NullInstallationOptionsFactory();
            }
            var index = chosen.Count();
            if (index == parts.Count)
            {
                return new NullInstallationOptionsFactory();
            }
            return await GetPlugin<IInstallationPluginOptionsFactory>(
                scope,
                filter: x => x,
                unusable: x => (!x.CanInstall(storeTypes), "This step cannot be used in combination with the specified store(s)"),
                defaultParam1: parts[index],
                defaultType: typeof(NullInstallationOptionsFactory),
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                nullResult: default,
#pragma warning restore CS8625
                className: "installation");
        }
    }
}
