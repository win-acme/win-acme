using Autofac;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.DomainObjects;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base.Factories.Null;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.StorePlugins;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Single = PKISharp.WACS.Plugins.OrderPlugins.Single;

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

        private async Task<Plugin?> GetPlugin<T>(
            ILifetimeScope scope,
            Steps step,
            Type defaultType,
            string className,
            string? defaultParam1 = null,
            string? defaultParam2 = null,
            Func<IEnumerable<PluginFactoryContext<T>>, IEnumerable<PluginFactoryContext<T>>>? filter = null,
            Func<PluginFactoryContext<T>, (bool, string?)>? unusable = null)
            where T : IPluginOptionsFactory
        {
            // Helper method to determine final usability state
            // combination of plugin being enabled (e.g. due to missing
            // administrator rights) and being a right fit for the current
            // renewal (e.g. cannot validate wildcards using http-01)
            (bool, string?) disabledOrUnusable(PluginFactoryContext<T> plugin)
            {
                var disabled = plugin.Factory.Disabled;
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
            IEnumerable<PluginFactoryContext<T>> options = new List<PluginFactoryContext<T>>();
            options = _plugins.
                GetPlugins(step).
                Select(x => new PluginFactoryContext<T>(x, scope)).
                ToList();
            options = filter != null ? filter(options) : options.Where(x => x is not INull);
            var localOptions = options.Select(x => new {
                plugin = x,
                disabled = disabledOrUnusable(x)
            });

            // Default out when there are no reasonable options to pick
            if (!localOptions.Any() ||
                localOptions.All(x => x.disabled.Item1) ||
                localOptions.All(x => x.plugin.Factory is INull))
            {
                return null;
            }

            var changeInstructions = $"Choose another plugin using the --{className} switch or change the default in settings.json";
            if (!string.IsNullOrEmpty(defaultParam1))
            {
                var defaultPlugin = _plugins.GetPlugin(step, defaultParam1, defaultParam2);
                if (defaultPlugin == null)
                {
                    _log.Error("Unable to find {n} plugin {p}. " + changeInstructions, className, defaultParam1);
                    return null;
                }
                else
                {
                    return defaultPlugin;
                }
            }

            var defaultOption = localOptions.First(x => x.plugin.Meta.Runner == defaultType);
            var defaultTypeDisabled = defaultOption.disabled;
            if (defaultTypeDisabled.Item1)
            {
                _log.Error("{n} plugin {x} not available: {m}. " + changeInstructions, 
                    step, 
                    defaultOption.plugin.Meta.Name ?? "Unknown",
                    defaultTypeDisabled.Item2?.TrimEnd('.'));
                return null;
            }

            return defaultOption.plugin.Meta;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// Renewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Plugin?> GetTargetPlugin(ILifetimeScope scope)
        {
            // NOTE: checking the default option here doesn't make 
            // sense because MainArguments.Source is what triggers
            // unattended mode in the first place. We woudn't even 
            // get into this code unless it was specified.
            return await GetPlugin<ITargetPluginOptionsFactory>(
                scope,
                Steps.Target,
                defaultParam1: string.IsNullOrWhiteSpace(_arguments.Source) ? _arguments.Target : _arguments.Source,
                defaultType: typeof(Manual),
                className: "source");
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this Renewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Plugin?> GetValidationPlugin(ILifetimeScope scope, Target target)
        {
            var defaultParam2 = _arguments.ValidationMode ?? _settings.Validation.DefaultValidationMode;
            if (string.IsNullOrEmpty(defaultParam2))
            {
                defaultParam2 = Constants.Http01ChallengeType;
            }
            return await GetPlugin<IValidationPluginOptionsFactory>(
                scope,
                Steps.Validation,
                defaultParam1: _arguments.Validation ?? _settings.Validation.DefaultValidation,
                defaultParam2: defaultParam2,
                defaultType: typeof(SelfHosting),
                unusable: (c) => (!c.Factory.CanValidate(target), "Unsupported source. Most likely this is because you have included a wildcard identifier (*.example.com), which requires DNS validation."),
                className: "validation");
        }

        /// <summary>
        /// Get the OrderPlugin which is used to convert the target into orders 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Plugin?> GetOrderPlugin(ILifetimeScope scope, Target target)
        {
            return await GetPlugin<IOrderPluginOptionsFactory>(
                scope,
                Steps.Order,
                defaultParam1: _arguments.Order,
                defaultType: typeof(Single),
                unusable: (c) => (!c.Factory.CanProcess(target), "Unsupported source. Most likely this is because you are using a custom CSR which doesn't allow the order to be split."),
                className: "order");
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Plugin?> GetCsrPlugin(ILifetimeScope scope)
        {
            return await GetPlugin<ICsrPluginOptionsFactory>(
                scope,
                Steps.Csr,
                defaultParam1: _arguments.Csr,
                defaultType: typeof(Rsa),
                className: "csr");
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Plugin?> GetStorePlugin(ILifetimeScope scope, IEnumerable<Plugin> chosen)
        {
            var nullResult = _plugins.
                GetPlugins(Steps.Store).
                Where(x => x.Runner == typeof(NullStore)).
                FirstOrDefault();
            var cmd = _arguments.Store ?? _settings.Store.DefaultStore;
            if (string.IsNullOrEmpty(cmd))
            {
                cmd = CertificateStoreOptions.PluginName;
            }
            var parts = cmd.ParseCsv();
            if (parts == null)
            {
                return nullResult;
            }
            var index = chosen.Count();
            if (index == parts.Count)
            {
                return nullResult;
            }
            return await GetPlugin<IStorePluginOptionsFactory>(
                scope,
                Steps.Store,
                filter: x => x,
                defaultParam1: parts[index],
                defaultType: typeof(NullStore),
                className: "store");
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public virtual async Task<Plugin?> GetInstallationPlugin(ILifetimeScope scope, IEnumerable<Plugin> storeTypes, IEnumerable<Plugin> chosen)
        {
            var cmd = _arguments.Installation ?? _settings.Installation.DefaultInstallation;
            var parts = cmd.ParseCsv();
            var nullResult = _plugins.
                GetPlugins(Steps.Installation).
                Where(x => x.Runner == typeof(NullInstallation)).
                FirstOrDefault();
            if (parts == null)
            {
                return nullResult;
            }
            var index = chosen.Count();
            if (index == parts.Count)
            {
                return nullResult;
            }
            return await GetPlugin<IInstallationPluginOptionsFactory>(
                scope,
                Steps.Installation,
                filter: x => x,
                unusable: x => { var (a, b) = x.Factory.CanInstall(storeTypes.Select(x => x.Runner), chosen.Select(x => x.Runner)); return (!a, b); },
                defaultParam1: parts[index],
                defaultType: typeof(NullInstallation),
                className: "installation");
        }
    }
}
