using Autofac;
using PKISharp.WACS.Configuration.Arguments;
using PKISharp.WACS.Extensions;
using PKISharp.WACS.Plugins.Base;
using PKISharp.WACS.Plugins.Base.Options;
using PKISharp.WACS.Plugins.CsrPlugins;
using PKISharp.WACS.Plugins.Interfaces;
using PKISharp.WACS.Plugins.TargetPlugins;
using PKISharp.WACS.Plugins.ValidationPlugins.Http;
using PKISharp.WACS.Services;
using PKISharp.WACS.Services.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PKISharp.WACS.Plugins.Resolvers
{
    internal class UnattendedResolver : IResolver
    {
        private readonly IPluginService _plugins;
        private readonly MainArguments _arguments;
        private readonly IAutofacBuilder _autofacBuilder;
        private readonly ISettingsService _settings;
        private readonly ILogService _log;
        private readonly ILifetimeScope _scope;

        public UnattendedResolver(
            ILogService log, 
            ISettingsService settings,
            IAutofacBuilder autofacBuilder,
            ILifetimeScope scope,
            MainArguments arguments,
            IPluginService pluginService)
        {
            _log = log;
            _plugins = pluginService;
            _arguments = arguments;
            _autofacBuilder = autofacBuilder;
            _scope = scope;
            _settings = settings;
        }

        [DebuggerDisplay("{Meta.Name}")]
        private record PluginChoice<TCapability, TOptions>(
            Plugin Meta,
            PluginFrontend<TCapability, TOptions> Frontend,
            State State)
            where TOptions : PluginOptions, new()
            where TCapability : IPluginCapability;

        private async Task<PluginFrontend<TCapability, TOptions>?> 
            GetPlugin<TCapability, TOptions>(
                Steps step,
                Type defaultBackend,
                string? defaultParam1 = null,
                string? defaultParam2 = null,
                Func<TCapability, State>? state = null)
                where TOptions : PluginOptions, new()
                where TCapability : IPluginCapability
        {
            // Helper method to determine final usability state
            // combination of plugin being enabled (e.g. due to missing
            // administrator rights) and being a right fit for the current
            // renewal (e.g. cannot validate wildcards using http-01)
            State combinedState(PluginFrontend<TCapability, TOptions> plugin)
            {
                var baseState = plugin.Capability.State;
                if (baseState.Disabled)
                {
                    return baseState;
                }
                else if (state != null)
                {
                    return state(plugin.Capability);
                }
                return State.EnabledState();
            };

            // Apply default sorting when no sorting has been provided yet
            var options = _plugins.
                GetPlugins(step).
                Select(x => _autofacBuilder.PluginFrontend<TCapability, TOptions>(_scope, x)).
                Select(x => x.Resolve<PluginFrontend<TCapability, TOptions>>()).
                Select(x => new PluginChoice<TCapability, TOptions>(x.Meta, x, combinedState(x))).
                ToList();

            // Default out when there are no reasonable plugins to pick
            if (!options.Any() || options.All(x => x.State.Disabled))
            {
                return null;
            }

            var className = step.ToString().ToLower();
            if (!string.IsNullOrEmpty(defaultParam1))
            {
                var defaultPlugin = _plugins.GetPlugin(step, defaultParam1, defaultParam2);
                if (defaultPlugin == null)
                {
                    _log.Error("Unable to find {n} plugin {p}. Choose another plugin using the {className} switch or change the default in settings.json", step, defaultParam1, $"--{className}");
                    return null;
                }
                else
                {
                    defaultBackend = defaultPlugin.Backend;
                }
            }

            var defaultOption = options.OrderBy(x => x.Meta.Hidden).First(x => x.Meta.Backend == defaultBackend);
            if (defaultOption.State.Disabled)
            {
                _log.Error("{n} plugin {x} not available: {m}. Choose another plugin using the {className} switch or change the default in settings.json", step, defaultOption.Frontend.Meta.Name ?? "Unknown", defaultOption.State.Reason?.TrimEnd('.'), $"--{className}");
                return null;
            }

            return defaultOption.Frontend;
        }

        /// <summary>
        /// Get the TargetPlugin which was used (or can be assumed to have been used) to create this
        /// Renewal
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<IPluginCapability, TargetPluginOptions>?> GetTargetPlugin()
        {
            // NOTE: checking the default option here doesn't make 
            // sense because MainArguments.Source is what triggers
            // unattended mode in the first place. We woudn't even 
            // get into this code unless it was specified.
            return await GetPlugin<IPluginCapability, TargetPluginOptions>(
                Steps.Source,
                defaultParam1: string.IsNullOrWhiteSpace(_arguments.Source) ? _arguments.Target : _arguments.Source,
                defaultBackend: typeof(Manual));
        }

        /// <summary>
        /// Get the ValidationPlugin which was used (or can be assumed to have been used) 
        /// to validate this Renewal
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<IValidationPluginCapability, ValidationPluginOptions>?> GetValidationPlugin()
        {
            return await GetPlugin<IValidationPluginCapability, ValidationPluginOptions>(
                Steps.Validation,
                defaultParam1: _arguments.Validation ?? _settings.Validation.DefaultValidation ?? "selfhosting",
                defaultParam2: _arguments.ValidationMode ?? _settings.Validation.DefaultValidationMode,
                defaultBackend: typeof(SelfHosting));
        }

        /// <summary>
        /// Get the OrderPlugin which is used to convert the target into orders 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<IPluginCapability, OrderPluginOptions>?> GetOrderPlugin()
        {
            return await GetPlugin<IPluginCapability, OrderPluginOptions>(
                Steps.Order,
                defaultParam1: _arguments.Order,
                defaultBackend: typeof(OrderPlugins.Single));
        }

        /// <summary>
        /// Get the CsrPlugin which is used to generate the private key 
        /// and request the certificate
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<IPluginCapability, CsrPluginOptions>?> GetCsrPlugin()
        {
            return await GetPlugin<IPluginCapability, CsrPluginOptions>(
                Steps.Csr,
                defaultParam1: _arguments.Csr,
                defaultBackend: typeof(Rsa));
        }

        /// <summary>
        /// Get the StorePlugin which is used to persist the certificate
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<IPluginCapability, StorePluginOptions>?> GetStorePlugin(IEnumerable<Plugin> chosen)
        {
            var defaultStore = _arguments.Store ?? _settings.Store.DefaultStore;
            if (string.IsNullOrWhiteSpace(defaultStore))
            {
                defaultStore = StorePlugins.CertificateStore.Name;
            }
            var parts = defaultStore.ParseCsv();
            if (parts == null)
            {
                return null;
            }
            var index = chosen.Count();
            defaultStore = index == parts.Count ? StorePlugins.Null.Name : parts[index];
            return await GetPlugin<IPluginCapability, StorePluginOptions>(
                Steps.Store,
                defaultParam1: defaultStore,
                defaultBackend: typeof(StorePlugins.Null));
        }

        /// <summary>
        /// Get the InstallationPlugin which was used (or can be assumed to have been used) to install 
        /// this ScheduledRenewal
        /// </summary>
        /// <returns></returns>
        public async Task<PluginFrontend<IInstallationPluginCapability, InstallationPluginOptions>?> 
            GetInstallationPlugin(IEnumerable<Plugin> stores, IEnumerable<Plugin> installation)
        {
            var defaultInstallation = _arguments.Installation ?? _settings.Installation.DefaultInstallation;
            var parts = defaultInstallation.ParseCsv();
            if (parts == null)
            {
                defaultInstallation = InstallationPlugins.Null.Name;
            } 
            else
            {
                var index = installation.Count();
                defaultInstallation = index == parts.Count ? InstallationPlugins.Null.Name : parts[index];
            }
            return await GetPlugin<IInstallationPluginCapability, InstallationPluginOptions>(
                Steps.Installation,
                state: x => x.CanInstall(stores.Select(x => x.Backend), installation.Select(x => x.Backend)),
                defaultParam1: defaultInstallation,
                defaultBackend: typeof(InstallationPlugins.Null));
        }
    }
}
